using System;
using System.Collections.Generic;
using System.Linq;
using XequeDeTinta.Battle;
using XequeDeTinta.Board;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Bot
{
    public class RunReport
    {
        public bool Won;
        public int Battles, PlayerTurns, Coins, DeckSize, Relics;
        public int HpBeforeBoss = -1;
        /// <summary>Casa onde morreu (-1 se venceu).</summary>
        public int DeathTile = -1;
        /// <summary>Soma de turnos do jogador por batalha (para estimar duração).</summary>
        public List<int> TurnsPerBattle = new List<int>();
    }

    /// <summary>Joga uma partida inteira em lógica pura (sem cenas) seguindo a BotPolicy. Determinístico pela semente.</summary>
    public static class RunSimulator
    {
        const int MaxTurnsPerBattle = 60;

        public static RunReport Play(GameConfig cfg, int seed)
        {
            var rng = new SeededRng(seed);
            var run = new RunState(cfg, rng);
            var graph = new BoardGraph(cfg.board.tiles);
            var mover = new BoardMover(graph, run.Progress.CurrentTile);
            var rep = new RunReport();

            for (int guard = 0; guard < 200; guard++)
            {
                int steps = DiceRoller.Roll(rng, cfg.diceSides);
                if (run.HasRelic(RelicKind.RerollDice) && steps <= 2) steps = DiceRoller.Roll(rng, cfg.diceSides);
                mover.Begin(steps);
                while (true)
                {
                    var step = mover.Advance();
                    if (step.kind == StepKind.Stopped) break;
                    if (step.kind == StepKind.ChoiceNeeded)
                        step = mover.Choose(BotPolicy.ChooseBranch(graph, step.options, run, rng));
                }
                run.Progress.CurrentTile = mover.Current;
                var tile = graph.Get(mover.Current);
                if (run.Progress.IsVisited(tile.id) || tile.type == TileType.Start) continue;

                if (tile.type == TileType.Boss) rep.HpBeforeBoss = run.Hp;
                if (!Resolve(cfg, run, tile, rng, rep))
                {
                    rep.DeathTile = tile.id; rep.Won = false; Finish(run, rep); return rep;
                }
                run.Progress.MarkVisited(tile.id);
                if (tile.type == TileType.Boss) { rep.Won = true; Finish(run, rep); return rep; }
            }
            Finish(run, rep);
            return rep;
        }

        static void Finish(RunState run, RunReport rep)
        {
            rep.Coins = run.Coins; rep.DeckSize = run.Deck.Count; rep.Relics = run.Relics.Count;
        }

        /// <summary>false se o herói morreu.</summary>
        static bool Resolve(GameConfig cfg, RunState run, BoardTile tile, IRng rng, RunReport rep)
        {
            switch (tile.type)
            {
                case TileType.Battle:
                case TileType.Boss:
                    var enc = tile.type == TileType.Boss ? cfg.bossEncounter : run.NextBattle();
                    if (!Fight(cfg, run, enc, rng, rep)) return false;
                    if (tile.type == TileType.Battle)
                    {
                        var reward = run.RollReward();
                        run.AddCoins(reward.Coins);
                        int pick = BotPolicy.ChooseReward(run, reward.Choices);
                        if (pick >= 0) run.AddCard(reward.Choices[pick]);
                    }
                    return true;
                case TileType.Rest: BotPolicy.UseRest(run); return true;
                case TileType.Shop: BotPolicy.UseShop(run, Shop.Create(run)); return true;
                case TileType.Chest: ChestLogic.Open(run); return true;
                case TileType.Event:
                    var ev = run.NextEvent();
                    var choice = BotPolicy.ChooseEventA(run, ev) ? ev.choiceA : ev.choiceB;
                    var res = EventLogic.Apply(run, choice);
                    if (res.RemoveCardPending) run.RemoveCard(BotPolicy.CardToRemove(run));
                    return true;
            }
            return true;
        }

        static bool Fight(GameConfig cfg, RunState run, EncounterData enc, IRng rng, RunReport rep)
        {
            var b = new BattleSystem(new BattleSetup
            {
                HeroHp = run.Hp, HeroMaxHp = run.MaxHp, Deck = new List<CardInstance>(run.Deck), Encounter = enc,
                Rules = BattleRules.From(cfg), Rng = new SeededRng(rng.Range(0, int.MaxValue)), Relics = new List<RelicData>(run.Relics),
            });
            b.Begin();
            int turns = 0;
            while (!b.IsOver && turns < MaxTurnsPerBattle)
            {
                turns++;
                int plays = 0;
                while (!b.IsOver && plays++ < 20)
                {
                    var move = BotPolicy.ChooseCard(b);
                    if (move == null) break;
                    b.PlayCard(move.Value.handIndex, move.Value.targetIndex).Resolve();
                }
                if (b.IsOver) break;
                b.EndPlayerTurn();
                b.RunEnemyTurn();
            }
            rep.Battles++; rep.PlayerTurns += turns; rep.TurnsPerBattle.Add(turns);
            run.Hp = Math.Max(0, b.Hero.Hp);
            return b.Phase == BattlePhase.Victory;
        }
    }
}
