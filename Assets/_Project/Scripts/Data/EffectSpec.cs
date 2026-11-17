using System;

namespace XequeDeTinta.Data
{
    /// <summary>Descrição serializável de um efeito de carta. Para ApplyBurn/ApplyWeak, value é o número de turnos.</summary>
    [Serializable]
    public struct EffectSpec
    {
        public EffectKind kind;
        public int value;
        /// <summary>Quantas vezes o efeito se repete (Golpe Duplo = 2).</summary>
        public int hits;

        public EffectSpec(EffectKind kind, int value, int hits = 1)
        {
            this.kind = kind;
            this.value = value;
            this.hits = hits;
        }
    }
}
