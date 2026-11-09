using UnityEngine;

namespace DiceDrive.Entities
{
    public enum EnemyAttackType
    {
        Melee,
        Ranged
    }

    public enum EnemySpeedType
    {
        Fast,
        Slow
    }

    public enum EnemyBodyType
    {
        Normal,
        Tank,
        Flying
    }

    [CreateAssetMenu(menuName = "Dice-Drive/Enemy", fileName = "Enemy")]
    public sealed class EnemyData : ScriptableObject
    {
        public string enemyName;
        public EnemyAttackType attackType;
        public EnemySpeedType speedType;
        public EnemyBodyType bodyType;
        public int maxHp = 1;
        public int damage = 1;
        public Sprite sprite;

        public bool CanBeJumpedOver => bodyType != EnemyBodyType.Flying;

        public bool BlocksMovement => attackType == EnemyAttackType.Ranged;

        public bool IgnoresSand => bodyType == EnemyBodyType.Flying;

        public bool IsMelee => attackType == EnemyAttackType.Melee;

        public bool IsFast => speedType == EnemySpeedType.Fast;
    }
}
