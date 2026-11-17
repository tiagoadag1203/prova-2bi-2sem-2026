using UnityEngine;

namespace XequeDeTinta.Data
{
    [CreateAssetMenu(menuName = "Xeque de Tinta/Relíquia", fileName = "Relic_Nova")]
    public class RelicData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public RelicKind kind;
        public int value;
        public Sprite icon;
    }
}
