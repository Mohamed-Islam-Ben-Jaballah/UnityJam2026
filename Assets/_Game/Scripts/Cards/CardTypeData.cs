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
        public Sprite icon;
    }
}
