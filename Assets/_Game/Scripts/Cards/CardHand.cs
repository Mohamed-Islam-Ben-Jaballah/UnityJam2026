using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceDrive.Cards
{
    /// <summary>
    /// The player's hand (plain C#): exactly 3 Ability cards (random, distinct characters) + 2 Basic Walk cards.
    /// Holds cards only; turn rules decide when they can be played.
    /// </summary>
    public sealed class CardHand
    {
        public const int AbilitySlots = 3;
        public const int WalkSlots = 2;
        public const int SlotCount = AbilitySlots + WalkSlots;

        private readonly List<CardData> _pool;
        private readonly CardData _walkCard;
        private readonly Random _random;
        private readonly CardData[] _slots = new CardData[SlotCount];

        /// <summary>Slots 0..2 are Ability cards, 3..4 are Basic Walk.</summary>
        public IReadOnlyList<CardData> Slots => _slots;

        public CardHand(IEnumerable<CardData> abilityPool, CardData walkCard, Random random)
        {
            _pool = abilityPool?.Where(c => c != null).Distinct().ToList() ?? throw new ArgumentNullException(nameof(abilityPool));
            if (_pool.Count < AbilitySlots) throw new ArgumentException($"Need at least {AbilitySlots} ability cards.", nameof(abilityPool));
            _walkCard = walkCard ?? throw new ArgumentNullException(nameof(walkCard));
            _random = random ?? new Random();
            Deal();
        }

        public static bool IsWalkSlot(int slot) => slot >= AbilitySlots;

        /// <summary>New hand: 3 random distinct abilities + 2 Basic Walk.</summary>
        public void Deal()
        {
            var picks = _pool.OrderBy(_ => _random.Next()).Take(AbilitySlots).ToList();
            for (var i = 0; i < AbilitySlots; i++) _slots[i] = picks[i];
            for (var i = AbilitySlots; i < SlotCount; i++) _slots[i] = _walkCard;
        }

    }
}
