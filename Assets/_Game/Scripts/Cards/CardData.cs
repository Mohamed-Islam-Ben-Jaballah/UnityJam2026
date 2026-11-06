using UnityEngine;

namespace DiceDrive.Cards
{
    public enum CardEffectType
    {
        Walk,
        Jump,
        Damage,
        Heal,
        RemoveObstacle,
        Draw,
        CrossDamage,
        Flood
    }

    public enum CardRangeType
    {
        Self,
        Melee,
        Move,
        Jump
    }

    [CreateAssetMenu(menuName = "Dice-Drive/Card", fileName = "Card")]
    public sealed class CardData : ScriptableObject
    {
        public string cardName;
        public CardTypeData cardType;
        public CardEffectType effect;
        public CardRangeType rangeType;
        public int rangeValue;
        public int damage;
        public int heal;
        public int drawCount;
        public int jumpDistance = 2;
        public Sprite art;
        [TextArea] public string description;
    }
}
