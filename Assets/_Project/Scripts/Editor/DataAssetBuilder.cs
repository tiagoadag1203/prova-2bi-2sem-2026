#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XequeDeTinta.Data;

namespace XequeDeTinta.EditorTools
{
    /// <summary>Cria (ou atualiza) todos os assets de dados a partir das tabelas do GDD. Idempotente.</summary>
    public static class DataAssetBuilder
    {
        const string Root = "Assets/_Project/Data/";

        [MenuItem("Xeque/M1/Criar assets de dados")]
        public static void BuildAll()
        {
            foreach (var d in new[] { "Cards", "Enemies", "Encounters", "Relics", "Events", "Boards" })
                Directory.CreateDirectory(Root + d);

            var cards = BuildCards();
            var enemies = BuildEnemies();
            var encounters = BuildEncounters(enemies);
            var relics = BuildRelics();
            var events = BuildEvents();
            var board = BuildBoard();

            var cfg = LoadOrCreate<GameConfig>(Root + "GameConfig.asset");
            cfg.starterDeck = new List<CardData>();
            Add(cfg.starterDeck, cards["golpe"], 5);
            Add(cfg.starterDeck, cards["defender"], 4);
            Add(cfg.starterDeck, cards["investida"], 1);
            cfg.rewardPool = new List<CardData>();
            foreach (var id in new[] { "bola_de_fogo", "brasa", "raiz", "casca_grossa", "onda", "respingo", "golpe_duplo", "seiva", "golpe_pesado" })
                cfg.rewardPool.Add(cards[id]);
            cfg.relics = relics;
            cfg.battleEncounters = encounters.normal;
            cfg.bossEncounter = encounters.boss;
            cfg.events = events;
            cfg.board = board;
            EditorUtility.SetDirty(cfg);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M1] Assets de dados criados em " + Root);
        }

        // ---------- Cartas ----------
        static Dictionary<string, CardData> BuildCards()
        {
            var d = new Dictionary<string, CardData>();
            EffectSpec Dmg(int v, int hits = 1) => new EffectSpec(EffectKind.DealDamage, v, hits);
            EffectSpec Shield(int v) => new EffectSpec(EffectKind.GainShield, v);
            EffectSpec Burn(int t) => new EffectSpec(EffectKind.ApplyBurn, t);
            EffectSpec Weak(int t) => new EffectSpec(EffectKind.ApplyWeak, t);
            EffectSpec Heal(int v) => new EffectSpec(EffectKind.Heal, v);
            EffectSpec Stun() => new EffectSpec(EffectKind.Stun, 1);

            void C(string id, string name, int cost, Element el, CardTarget tg, EffectSpec[] fx, EffectSpec[] up,
                   string desc, string upDesc, bool exhaust = false, bool pool = false)
            {
                var c = LoadOrCreate<CardData>(Root + "Cards/Card_" + Pascal(id) + ".asset");
                c.id = id; c.displayName = name; c.cost = cost; c.element = el; c.target = tg;
                c.effects = new List<EffectSpec>(fx); c.upgradedEffects = new List<EffectSpec>(up);
                c.description = desc; c.upgradedDescription = upDesc; c.exhaust = exhaust; c.inRewardPool = pool;
                c.art = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Cards/card_" + id + ".png");
                EditorUtility.SetDirty(c);
                d[id] = c;
            }

            var N = Element.Neutral; var one = CardTarget.SingleEnemy; var self = CardTarget.Self; var all = CardTarget.AllEnemies;
            C("golpe", "Golpe", 1, N, one, new[] { Dmg(6) }, new[] { Dmg(9) }, "Causa 6 de dano.", "Causa 9 de dano.");
            C("defender", "Defender", 1, N, self, new[] { Shield(5) }, new[] { Shield(8) }, "Ganha 5 de escudo.", "Ganha 8 de escudo.");
            C("investida", "Investida", 2, N, one, new[] { Dmg(10), Stun() }, new[] { Dmg(14), Stun() }, "Causa 10 de dano e atordoa.", "Causa 14 de dano e atordoa.");
            C("bola_de_fogo", "Bola de Fogo", 1, Element.Fire, one, new[] { Dmg(7) }, new[] { Dmg(10) }, "Causa 7 de dano.", "Causa 10 de dano.", pool: true);
            C("brasa", "Brasa", 1, Element.Fire, one, new[] { Burn(3) }, new[] { Burn(5) }, "Aplica Queimadura 3.", "Aplica Queimadura 5.", pool: true);
            C("raiz", "Raiz", 1, Element.Plant, one, new[] { Dmg(4), Shield(4) }, new[] { Dmg(6), Shield(6) }, "Causa 4 de dano e ganha 4 de escudo.", "Causa 6 de dano e ganha 6 de escudo.", pool: true);
            C("casca_grossa", "Casca Grossa", 1, Element.Plant, self, new[] { Shield(8) }, new[] { Shield(11) }, "Ganha 8 de escudo.", "Ganha 11 de escudo.", pool: true);
            C("onda", "Onda", 2, Element.Water, all, new[] { Dmg(5) }, new[] { Dmg(8) }, "Causa 5 de dano em todos.", "Causa 8 de dano em todos.", pool: true);
            C("respingo", "Respingo", 1, Element.Water, one, new[] { Dmg(4), Weak(1) }, new[] { Dmg(6), Weak(2) }, "Causa 4 de dano e aplica Fraco 1.", "Causa 6 de dano e aplica Fraco 2.", pool: true);
            C("golpe_duplo", "Golpe Duplo", 1, N, one, new[] { Dmg(4, 2) }, new[] { Dmg(5, 2) }, "Causa 4 de dano duas vezes.", "Causa 5 de dano duas vezes.", pool: true);
            C("seiva", "Seiva", 1, Element.Plant, self, new[] { Heal(6) }, new[] { Heal(9) }, "Cura 6. Esgota.", "Cura 9. Esgota.", exhaust: true, pool: true);
            C("golpe_pesado", "Golpe Pesado", 2, N, one, new[] { Dmg(14) }, new[] { Dmg(18) }, "Causa 14 de dano.", "Causa 18 de dano.", pool: true);
            return d;
        }

        // ---------- Inimigos ----------
        class Enemies { public EnemyData green, red, blue, king, mini; }

        static EnemyData Enemy(string id, string name, Element el, int hp, EnemyBehaviorKind behavior, string spriteName,
                               string controller, float scale, params EnemyAction[] actions)
        {
            var e = LoadOrCreate<EnemyData>(Root + "Enemies/Enemy_" + Pascal(id) + ".asset");
            e.id = id; e.displayName = name; e.element = el; e.maxHp = hp; e.behavior = behavior;
            e.actions = new List<EnemyAction>(actions); e.spriteScale = scale;
            e.sprite = string.IsNullOrEmpty(spriteName) ? null : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/" + spriteName + ".png");
            e.animator = string.IsNullOrEmpty(controller) ? null : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/_Project/Animations/{controller}/{controller}.controller");
            return e;
        }

        static EnemyAction Act(string name, IntentType intent, int damage = 0, int shield = 0, int burn = 0, int heal = 0, int summon = 0, int strength = 0)
            => new EnemyAction { name = name, intent = intent, damage = damage, shield = shield, burnTurns = burn, heal = heal, summonCount = summon, strength = strength };

        static Enemies BuildEnemies()
        {
            var r = new Enemies();
            r.green = Enemy("slime_verde", "Slime Verde", Element.Plant, 18, EnemyBehaviorKind.RandomNoRepeatDefend,
                "Enemies/slime_green_idle_0", "slime_green", 1f,
                Act("Investida", IntentType.Attack, damage: 5), Act("Casca", IntentType.Defend, shield: 4));
            r.red = Enemy("slime_vermelho", "Slime Vermelho", Element.Fire, 14, EnemyBehaviorKind.EveryNthSpecial,
                "Enemies/slime_red_idle_0", "slime_red", 1f,
                Act("Investida", IntentType.Attack, damage: 7), Act("Chama", IntentType.Debuff, burn: 2));
            r.red.specialEveryN = 3;
            r.blue = Enemy("slime_azul", "Slime Azul", Element.Water, 22, EnemyBehaviorKind.Simple,
                "Enemies/slime_blue_idle_0", "slime_blue", 1f,
                Act("Respingo", IntentType.Attack, damage: 4, heal: 3));
            r.mini = Enemy("slime_mirim", "Slime Mirim", Element.Plant, 8, EnemyBehaviorKind.Simple,
                "Enemies/slime_green_idle_0", "slime_green", 0.6f,
                Act("Cutucada", IntentType.Attack, damage: 3));
            r.king = Enemy("rei_slime", "Rei Slime", Element.Plant, 70, EnemyBehaviorKind.Cycle, "Bosses/king_slime_idle_0", "king_slime", 1f,
                Act("Esmagar", IntentType.Attack, damage: 12),
                Act("Dividir", IntentType.Summon, summon: 2),
                Act("Coroa Pesada", IntentType.Attack, damage: 6, shield: 8));
            r.king.isBoss = true;
            r.king.enrageBelowHpFraction = 0.5f;
            r.king.enrageStrength = 2;
            r.king.summonedEnemy = r.mini;
            r.king.summonMaxAlive = 2;
            foreach (var e in new[] { r.green, r.red, r.blue, r.mini, r.king }) EditorUtility.SetDirty(e);
            return r;
        }

        // ---------- Encontros ----------
        static (List<EncounterData> normal, EncounterData boss) BuildEncounters(Enemies e)
        {
            EncounterData Enc(string id, bool boss, params EnemyData[] list)
            {
                var enc = LoadOrCreate<EncounterData>(Root + "Encounters/Encounter_" + Pascal(id) + ".asset");
                enc.id = id; enc.isBoss = boss; enc.enemies = new List<EnemyData>(list);
                EditorUtility.SetDirty(enc);
                return enc;
            }
            var normal = new List<EncounterData>
            {
                Enc("batalha_1", false, e.green),
                Enc("batalha_2", false, e.red),
                Enc("batalha_3", false, e.green, e.blue),
                Enc("batalha_4", false, e.red, e.green),
                Enc("batalha_5", false, e.blue, e.red, e.green),
            };
            return (normal, Enc("chefe", true, e.king));
        }

        // ---------- Relíquias ----------
        static List<RelicData> BuildRelics()
        {
            RelicData R(string id, string name, string desc, RelicKind kind, int value)
            {
                var r = LoadOrCreate<RelicData>(Root + "Relics/Relic_" + Pascal(id) + ".asset");
                r.id = id; r.displayName = name; r.description = desc; r.kind = kind; r.value = value;
                r.icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Relics/relic_" + id + ".png");
                EditorUtility.SetDirty(r);
                return r;
            }
            return new List<RelicData>
            {
                R("dado_viciado", "Dado Viciado", "Pode rolar o dado de novo 1 vez.", RelicKind.RerollDice, 1),
                R("base_de_chumbo", "Base de Chumbo", "Começa toda batalha com 5 de escudo.", RelicKind.StartShield, 5),
                R("tinta_seca", "Tinta Seca", "Ao matar um inimigo, cura 3.", RelicKind.HealOnKill, 3),
                R("pincel_afiado", "Pincel Afiado", "Começa toda batalha com 1 de Força.", RelicKind.StartStrength, 1),
                R("ampulheta_de_bolso", "Ampulheta de Bolso", "No primeiro turno de cada batalha, +1 de energia.", RelicKind.FirstTurnEnergy, 1),
            };
        }

        // ---------- Eventos ----------
        static List<EventData> BuildEvents()
        {
            EventOutcome O(EventOutcomeKind k, int v = 0, int v2 = 0) => new EventOutcome { kind = k, value = v, value2 = v2 };
            EventChoice Ch(string text, params EventOutcome[] o) => new EventChoice { text = text, outcomes = new List<EventOutcome>(o) };
            EventData Ev(string id, string title, string body, EventChoice a, EventChoice b)
            {
                var e = LoadOrCreate<EventData>(Root + "Events/Event_" + Pascal(id) + ".asset");
                e.id = id; e.title = title; e.body = body; e.choiceA = a; e.choiceB = b;
                EditorUtility.SetDirty(e);
                return e;
            }
            return new List<EventData>
            {
                Ev("poca_de_tinta", "Poça de Tinta",
                   "Uma poça de tinta brilhante borbulha no meio do caminho. Ela parece cheia de poder, mas também de perigo.",
                   Ch("Beber (perde 6 de vida, melhora 1 carta)", O(EventOutcomeKind.LoseHp, 6), O(EventOutcomeKind.GainRandomUpgradedCard)),
                   Ch("Seguir em frente", O(EventOutcomeKind.Nothing))),
                Ev("dado_gigante", "Dado Gigante",
                   "Um dado enorme descansa sobre a casa. Dizem que ele decide o destino de quem o rola.",
                   Ch("Rolar (1-2 perde 8 de vida, 3-4 ganha 30 moedas, 5-6 ganha 1 relíquia)", O(EventOutcomeKind.DiceGamble, 8, 30)),
                   Ch("Não rolar", O(EventOutcomeKind.Nothing))),
                Ev("fonte_de_feltro", "Fonte de Feltro",
                   "Uma fonte de feltro macio pulsa com uma luz tranquila. Você sente que ela pode curar ou desfazer algo.",
                   Ch("Beber (cura 12)", O(EventOutcomeKind.Heal, 12)),
                   Ch("Remover 1 carta do deck", O(EventOutcomeKind.RemoveCard))),
            };
        }

        // ---------- Tabuleiro ----------
        static BoardLayout BuildBoard()
        {
            var b = LoadOrCreate<BoardLayout>(Root + "Boards/Board_Floresta.asset");
            b.tiles = new List<BoardTile>();
            void T(int id, TileType t, float x, float y, params int[] next)
                => b.tiles.Add(new BoardTile { id = id, type = t, position = new Vector2(x, y), next = new List<int>(next) });
            T(0, TileType.Start, -7f, 0f, 1);
            T(1, TileType.Battle, -5.5f, 0f, 2);
            T(2, TileType.Event, -4f, 0f, 3, 4);   // bifurcação 1
            T(3, TileType.Battle, -2.5f, 1.4f, 5);
            T(4, TileType.Rest, -2.5f, -1.4f, 5);
            T(5, TileType.Battle, -1f, 0f, 6);
            T(6, TileType.Shop, 0.5f, 0f, 7, 8);   // bifurcação 2
            T(7, TileType.Battle, 2f, 1.4f, 9);
            T(8, TileType.Chest, 2f, -1.4f, 9);
            T(9, TileType.Event, 3.5f, 0f, 10);
            T(10, TileType.Battle, 5f, 0f, 11);
            T(11, TileType.Boss, 6.5f, 0f);
            b.startTileId = 0;
            EditorUtility.SetDirty(b);
            return b;
        }

        // ---------- Utilidades ----------
        static void Add(List<CardData> list, CardData c, int n) { for (int i = 0; i < n; i++) list.Add(c); }

        static string Pascal(string id)
        {
            var parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join("", parts);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }
    }
}
#endif
