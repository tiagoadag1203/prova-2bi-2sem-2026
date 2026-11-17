using System.Collections.Generic;
using XequeDeTinta.Core;

namespace XequeDeTinta.Battle
{
    /// <summary>Compra, mão, descarte e esgotadas. Topo do baralho de compra = fim da lista.</summary>
    public class DeckManager
    {
        readonly IRng rng;
        public readonly List<CardInstance> DrawPile = new List<CardInstance>();
        public readonly List<CardInstance> Hand = new List<CardInstance>();
        public readonly List<CardInstance> DiscardPile = new List<CardInstance>();
        public readonly List<CardInstance> ExhaustPile = new List<CardInstance>();

        public DeckManager(IEnumerable<CardInstance> cards, IRng rng)
        {
            this.rng = rng;
            DrawPile.AddRange(cards);
            rng.Shuffle(DrawPile);
        }

        /// <summary>Compra até n cartas; se o baralho esvazia, embaralha o descarte e continua. Devolve quantas comprou.</summary>
        public int Draw(int n)
        {
            int drawn = 0;
            for (int i = 0; i < n; i++)
            {
                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count == 0) break;
                    DrawPile.AddRange(DiscardPile);
                    DiscardPile.Clear();
                    rng.Shuffle(DrawPile);
                }
                var c = DrawPile[DrawPile.Count - 1];
                DrawPile.RemoveAt(DrawPile.Count - 1);
                Hand.Add(c);
                drawn++;
            }
            return drawn;
        }

        public void DiscardHand()
        {
            DiscardPile.AddRange(Hand);
            Hand.Clear();
        }

        /// <summary>Tira a carta da mão e a manda para o descarte ou, se esgota, para fora da batalha.</summary>
        public CardInstance PlayFromHand(int handIndex)
        {
            var c = Hand[handIndex];
            Hand.RemoveAt(handIndex);
            return c;
        }

        public void Finish(CardInstance card)
        {
            if (card.Exhaust) ExhaustPile.Add(card); else DiscardPile.Add(card);
        }

        public int TotalCards => DrawPile.Count + Hand.Count + DiscardPile.Count + ExhaustPile.Count;
    }
}
