using System;
using System.Collections.Generic;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    public enum BattlePhase { Start, PlayerTurn, EnemyTurn, Victory, Defeat }

    public class BattleSetup
    {
        public int HeroHp = 50, HeroMaxHp = 50;
        public List<CardInstance> Deck = new List<CardInstance>();
        public EncounterData Encounter;
        public BattleRules Rules = new BattleRules();
        public IRng Rng = new SeededRng();
        /// <summary>Relíquias do herói; cada uma vira um gancho (IBattleHook) que reage à batalha.</summary>
        public List<RelicData> Relics = new List<RelicData>();
    }

    /// <summary>Ganchos de relíquia: classes pequenas que reagem a momentos da batalha e devolvem eventos para a apresentação.</summary>
    public interface IBattleHook
    {
        /// <summary>No início de cada turno do jogador, depois do escudo zerar e antes de comprar. 'first' é o primeiro turno da batalha.</summary>
        void OnPlayerTurnStart(BattleSystem battle, bool first, List<BattleEvent> events);
        void OnEnemyKilled(BattleSystem battle, Enemy enemy, List<BattleEvent> events);
    }

    /// <summary>
    /// Máquina de estados da batalha: Início, TurnoJogador, TurnoInimigo, Vitória e Derrota.
    /// Lógica pura: a apresentação chama PlayCard / EndPlayerTurn / NextEnemyAction e usa os eventos devolvidos.
    /// </summary>
    public class BattleSystem
    {
        public BattleRules Rules { get; }
        public IRng Rng { get; }
        public Hero Hero { get; }
        public List<Enemy> Enemies { get; } = new List<Enemy>();
        public DeckManager Deck { get; }
        public BattlePhase Phase { get; private set; } = BattlePhase.Start;
        public int Energy { get; set; }
        public int Turn { get; private set; }
        public readonly List<IBattleHook> Hooks = new List<IBattleHook>();
        /// <summary>Eventos do início do último turno do jogador (Queimadura do herói), para a apresentação mostrar.</summary>
        public readonly List<BattleEvent> TurnStartEvents = new List<BattleEvent>();

        public event Action<BattlePhase> PhaseChanged;

        readonly Queue<Enemy> enemyQueue = new Queue<Enemy>();

        public BattleSystem(BattleSetup setup)
        {
            Rules = setup.Rules;
            Rng = setup.Rng;
            Hero = new Hero { Name = "Peão", Element = Element.Neutral, MaxHp = setup.HeroMaxHp, Hp = setup.HeroHp };
            Deck = new DeckManager(setup.Deck, Rng);
            foreach (var data in setup.Encounter.enemies) SpawnEnemy(data);
            foreach (var relic in setup.Relics)
            {
                var hook = RelicHooks.Create(relic);
                if (hook != null) Hooks.Add(hook);
            }
        }

        public bool IsOver => Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat;
        public IEnumerable<Enemy> AliveEnemies { get { foreach (var e in Enemies) if (e.IsAlive) yield return e; } }
        public int AliveCount { get { int n = 0; foreach (var e in Enemies) if (e.IsAlive) n++; return n; } }

        void SetPhase(BattlePhase p) { Phase = p; PhaseChanged?.Invoke(p); }

        // ---------- Início ----------

        public Enemy SpawnEnemy(EnemyData data)
        {
            var e = new Enemy { Data = data, Name = data.displayName, Element = data.element, MaxHp = data.maxHp, Hp = data.maxHp };
            EnemyBrain.ChooseIntent(e, Rng);
            Enemies.Add(e);
            return e;
        }

        public void Begin()
        {
            StartPlayerTurn();
        }

        // ---------- Turno do jogador ----------

        void StartPlayerTurn()
        {
            Turn++;
            SetPhase(BattlePhase.PlayerTurn);
            Hero.Shield = 0;
            TurnStartEvents.Clear();
            TickBurn(Hero, TurnStartEvents);
            AnnounceDeaths(TurnStartEvents);
            if (CheckEnd()) return;

            Energy = Rules.EnergyPerTurn;
            foreach (var h in Hooks) h.OnPlayerTurnStart(this, Turn == 1, TurnStartEvents);
            Deck.Draw(Rules.HandSize);
        }

        public bool CanPlay(int handIndex)
        {
            if (Phase != BattlePhase.PlayerTurn) return false;
            if (handIndex < 0 || handIndex >= Deck.Hand.Count) return false;
            return Deck.Hand[handIndex].Cost <= Energy;
        }

        /// <summary>
        /// Gasta a energia e tira a carta da mão; os efeitos só são aplicados em Resolve() (no quadro de impacto da animação).
        /// targetIndex é o índice em Enemies; ignorado quando a carta não pede alvo.
        /// </summary>
        public CardPlay PlayCard(int handIndex, int targetIndex = -1)
        {
            if (!CanPlay(handIndex)) throw new InvalidOperationException("Jogada inválida: fase, índice ou energia insuficiente.");
            var card = Deck.Hand[handIndex];
            if (card.Data.target == CardTarget.SingleEnemy)
            {
                if (targetIndex < 0 || targetIndex >= Enemies.Count || !Enemies[targetIndex].IsAlive)
                    throw new InvalidOperationException("A carta precisa de um alvo vivo.");
            }

            Energy -= card.Cost;
            Deck.PlayFromHand(handIndex);

            var targets = new List<Combatant>();
            if (card.Data.target == CardTarget.SingleEnemy) targets.Add(Enemies[targetIndex]);
            else if (card.Data.target == CardTarget.AllEnemies) foreach (var e in AliveEnemies) targets.Add(e);
            return new CardPlay(this, card, targets);
        }

        public sealed class CardPlay
        {
            readonly BattleSystem battle;
            public CardInstance Card { get; }
            public IReadOnlyList<Combatant> Targets { get; }
            public bool Resolved { get; private set; }

            internal CardPlay(BattleSystem battle, CardInstance card, List<Combatant> targets)
            {
                this.battle = battle; Card = card; Targets = targets;
            }

            public List<BattleEvent> Resolve()
            {
                var events = new List<BattleEvent>();
                if (Resolved) return events;
                Resolved = true;
                var ctx = new EffectContext { Rules = battle.Rules, Element = Card.Element };
                foreach (var spec in Card.Effects)
                {
                    var effect = EffectFactory.Create(spec);
                    if (effect.TargetsUser) effect.Apply(ctx, battle.Hero, battle.Hero, events);
                    else foreach (var t in Targets) effect.Apply(ctx, battle.Hero, t, events);
                }
                battle.Deck.Finish(Card);
                battle.AfterResolve(events);
                return events;
            }
        }

        public void EndPlayerTurn()
        {
            if (Phase != BattlePhase.PlayerTurn) return;
            if (Hero.Weak > 0) Hero.Weak--;
            Deck.DiscardHand();
            SetPhase(BattlePhase.EnemyTurn);
            foreach (var e in Enemies) e.Shield = 0; // escudo dos inimigos zera no início do turno deles
            enemyQueue.Clear();
            foreach (var e in Enemies) if (e.IsAlive) enemyQueue.Enqueue(e);
        }

        // ---------- Turno dos inimigos ----------

        public sealed class EnemyPlay
        {
            readonly BattleSystem battle;
            public Enemy Enemy { get; }
            public EnemyAction Action { get; }
            /// <summary>Queimadura e atordoamento, resolvidos antes da ação.</summary>
            public List<BattleEvent> PreEvents { get; }
            /// <summary>Não age neste turno (atordoado ou morto pela Queimadura).</summary>
            public bool Skipped { get; }
            public bool Resolved { get; private set; }

            internal EnemyPlay(BattleSystem battle, Enemy enemy, EnemyAction action, List<BattleEvent> pre, bool skipped)
            {
                this.battle = battle; Enemy = enemy; Action = action; PreEvents = pre; Skipped = skipped;
            }

            public List<BattleEvent> Resolve()
            {
                var events = new List<BattleEvent>();
                if (Resolved) return events;
                Resolved = true;
                if (!Skipped)
                {
                    foreach (var fx in EnemyEffectFactory.Build(Action)) fx.Apply(battle, Enemy, events);
                    if (Enemy.Weak > 0) Enemy.Weak--;
                    Enemy.LastIndex = Enemy.IntentIndex;
                    Enemy.TurnsActed++;
                    EnemyBrain.ChooseIntent(Enemy, battle.Rng);
                }
                battle.AfterResolve(events);
                return events;
            }
        }

        /// <summary>
        /// Próximo inimigo a agir (da esquerda para a direita), ou null quando o turno dos inimigos acabou
        /// (neste caso começa o turno do jogador, a menos que a batalha tenha terminado).
        /// </summary>
        public EnemyPlay NextEnemyAction()
        {
            if (Phase != BattlePhase.EnemyTurn) return null;
            while (enemyQueue.Count > 0)
            {
                var e = enemyQueue.Dequeue();
                if (!e.IsAlive) continue;

                var pre = new List<BattleEvent>();
                TickBurn(e, pre);
                AnnounceDeaths(pre);
                if (!e.IsAlive)
                {
                    if (CheckEnd()) return new EnemyPlay(this, e, null, pre, true);
                    return new EnemyPlay(this, e, null, pre, true);
                }
                if (e.Stunned)
                {
                    e.Stunned = false; // a intenção fica para o turno seguinte
                    pre.Add(new BattleEvent(BattleEventKind.StunSkip, e, e));
                    return new EnemyPlay(this, e, null, pre, true);
                }
                return new EnemyPlay(this, e, e.Intent, pre, false);
            }

            if (!IsOver) StartPlayerTurn();
            return null;
        }

        /// <summary>Resolve o turno inteiro dos inimigos de uma vez (testes e simulação).</summary>
        public List<BattleEvent> RunEnemyTurn()
        {
            var all = new List<BattleEvent>();
            EnemyPlay play;
            while ((play = NextEnemyAction()) != null)
            {
                all.AddRange(play.PreEvents);
                all.AddRange(play.Resolve());
                if (IsOver) break;
            }
            return all;
        }

        // ---------- Comum ----------

        /// <summary>Queimadura: no início do turno do alvo perde vida (ignora escudo) e gasta um turno.</summary>
        void TickBurn(Combatant c, List<BattleEvent> events)
        {
            if (c.Burn <= 0) return;
            int lost = c.LoseHp(Rules.BurnDamage);
            c.Burn--;
            events.Add(new BattleEvent(BattleEventKind.BurnTick, c, c, lost));
        }

        void AfterResolve(List<BattleEvent> events)
        {
            CheckEnrage(events);
            AnnounceDeaths(events);
            CheckEnd();
        }

        /// <summary>Chefe abaixo de 50% de vida ganha Força uma única vez.</summary>
        void CheckEnrage(List<BattleEvent> events)
        {
            foreach (var e in Enemies)
            {
                var d = e.Data;
                if (e.Enraged || !e.IsAlive || d.enrageBelowHpFraction <= 0f) continue;
                if (e.Hp < e.MaxHp * d.enrageBelowHpFraction)
                {
                    e.Enraged = true;
                    e.Strength += d.enrageStrength;
                    events.Add(new BattleEvent(BattleEventKind.Enrage, e, e, d.enrageStrength));
                }
            }
        }

        void AnnounceDeaths(List<BattleEvent> events)
        {
            foreach (var e in Enemies)
                if (!e.IsAlive && !e.DeathAnnounced)
                {
                    e.DeathAnnounced = true;
                    events.Add(new BattleEvent(BattleEventKind.Death, Hero, e));
                    foreach (var h in Hooks) h.OnEnemyKilled(this, e, events);
                }
            if (!Hero.IsAlive && !Hero.DeathAnnounced)
            {
                Hero.DeathAnnounced = true;
                events.Add(new BattleEvent(BattleEventKind.Death, null, Hero));
            }
        }

        bool CheckEnd()
        {
            if (IsOver) return true;
            if (!Hero.IsAlive) { SetPhase(BattlePhase.Defeat); return true; }
            if (AliveCount == 0) { SetPhase(BattlePhase.Victory); return true; }
            return false;
        }

        /// <summary>Dano que a intenção do inimigo causará agora (para o número no ícone de intenção).</summary>
        public int PreviewDamage(Enemy e)
        {
            if (e.Intent == null || e.Intent.damage <= 0) return 0;
            return DamageCalc.Calculate(e.Intent.damage, e.Element, e.Strength, e.Weak > 0, Hero.Element, Rules).amount;
        }
    }
}
