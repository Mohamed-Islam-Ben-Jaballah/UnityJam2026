using UnityEngine;

namespace DiceDrive.Cards
{
    public enum CardTypeId
    {
        Normal,
        Elvish,
        Devilish,
        Sinitic,
        Loneliness
    }

    [CreateAssetMenu(menuName = "Dice-Drive/Card Type", fileName = "CardType")]
    public sealed class CardTypeData : ScriptableObject
    {
        public CardTypeId typeId;
        public int luckyNumber;
        public string displayName;

        [Tooltip("Faction theme shown on cards, e.g. Union, Desire, Fortune.")]
        public string theme;

        [Tooltip("Placeholder card colour for this faction.")]
        public Color color = Color.gray;

        public Sprite icon;
    }
}
