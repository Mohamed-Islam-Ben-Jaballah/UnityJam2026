using UnityEngine;

namespace DiceDrive.Cards
{
    public enum CardTypeId
    {
        Normal,
        Elf,
        Devil,
        China,
        Angel,
        Desert
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
