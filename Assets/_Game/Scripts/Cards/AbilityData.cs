using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>What an ability does. One shared implementation per kind (all Damage cards behave the same, etc.).</summary>
    public enum AbilityKind
    {
        Walk,
        Damage,
        Heal,
        Jump,
        /// <summary>Card management; behaviour not finalised yet — activation hook only.</summary>
        Withdraw
    }

    /// <summary>
    /// An ability effect, independent of which character carries it (e.g. "Damage 1" is shared by
    /// Squimi, Sara'akh and Sunhai). Cards reference one of these via <see cref="CardData.ability"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Dice-Drive/Ability", fileName = "Ability")]
    public sealed class AbilityData : ScriptableObject
    {
        public string abilityName = "Damage";
        public AbilityKind kind = AbilityKind.Damage;

        [Tooltip("Damage dealt, HP healed, tiles jumped over, or cards withdrawn.")]
        public int value = 1;

        public string Label => kind == AbilityKind.Walk ? abilityName : $"{abilityName} {value}";
    }
}
