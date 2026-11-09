using UnityEngine;

namespace DiceDrive.DiceSystem
{
    [CreateAssetMenu(menuName = "Dice-Drive/Die", fileName = "Die")]
    public sealed class DiceData : ScriptableObject
    {
        public int sides = 4;
    }
}
