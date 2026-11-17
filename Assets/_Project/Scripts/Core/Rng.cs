namespace XequeDeTinta.Core
{
    /// <summary>Fonte de aleatoriedade injetável: dado, embaralhamento, IA e sorteios usam isto, para os testes serem determinísticos.</summary>
    public interface IRng
    {
        /// <summary>Inteiro em [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
        /// <summary>Número em [0, 1).</summary>
        float Value();
    }

    public sealed class SeededRng : IRng
    {
        readonly System.Random random;

        public SeededRng() { random = new System.Random(); }
        public SeededRng(int seed) { random = new System.Random(seed); }

        public int Range(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
        public float Value() => (float)random.NextDouble();
    }

    /// <summary>Aleatoriedade roteirizada para testes: devolve os valores da fila (cada um limitado ao intervalo pedido) e depois o mínimo.</summary>
    public sealed class ScriptedRng : IRng
    {
        readonly System.Collections.Generic.Queue<int> values;

        public ScriptedRng(params int[] values)
        {
            this.values = new System.Collections.Generic.Queue<int>(values);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (values.Count == 0) return minInclusive;
            int v = values.Dequeue();
            if (v < minInclusive) v = minInclusive;
            if (v >= maxExclusive) v = maxExclusive - 1;
            return v;
        }

        public float Value() => values.Count == 0 ? 0f : values.Dequeue() / 100f;
    }

    public static class RngExtensions
    {
        /// <summary>Fisher-Yates.</summary>
        public static void Shuffle<T>(this IRng rng, System.Collections.Generic.IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
