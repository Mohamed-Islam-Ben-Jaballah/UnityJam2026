using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.UI;
using UnityEngine;

namespace DiceDrive.Board
{
    /// <summary>
    /// Connects the hand (3 Ability + 2 Basic Walk) to the existing card UI (HandView / CardView / CardRevealView)
    /// and to the turn. Cards are used up when played (no refill).
    ///  - Basic Walk: only on a roll of 1, optional; each one played gives +1 Focus (max 2).
    ///  - When an enemy blocks the walk: an Ability card whose faction number matches the roll can be played
    ///    (Damage kills, Jump hops over). With full Focus any Ability card can be played, spending the Focus.
    ///    With no usable card the player takes the hit automatically.
    /// </summary>
    public sealed class CardPathActions : MonoBehaviour
    {
        [SerializeField] private CardSelectionService service;
        [SerializeField] private DiceBoardMovement movement;
        [SerializeField] private FocusView focusView;

        [Header("Hand")]
        [Tooltip("The Ability cards the hand draws from.")]
        [SerializeField] private CardData[] abilityPool;
        [SerializeField] private CardData basicWalkCard;
        [Tooltip("0 = random every play.")]
        [SerializeField] private int randomSeed;

        [Header("Focus")]
        [SerializeField, Min(1)] private int maxFocus = 2;
        [Tooltip("Focus needed (and spent) to play an Ability card whose faction doesn't match the roll.")]
        [SerializeField, Min(1)] private int focusCost = 2;
        [SerializeField, Min(1)] private int walkRoll = 1;

        private CardHand _hand;
        private readonly string[] _slotIds = new string[CardHand.SlotCount];
        private readonly Dictionary<string, int> _slotById = new Dictionary<string, int>();
        private readonly HashSet<int> _usedSlots = new HashSet<int>();
        private int _focus;

        public CardHand Hand => _hand;
        public int Focus => _focus;

        private void Start()
        {
            if (service == null) service = FindAnyObjectByType<CardSelectionService>();
            if (movement == null) movement = FindAnyObjectByType<DiceBoardMovement>();
            if (focusView == null) focusView = FindAnyObjectByType<FocusView>();
            if (service == null || movement == null || basicWalkCard == null || abilityPool == null || abilityPool.Length < CardHand.AbilitySlots)
            {
                Debug.LogError("[CardPathActions] Needs CardSelectionService, DiceBoardMovement, a Basic Walk card and at least 3 ability cards.", this);
                enabled = false;
                return;
            }

            _hand = new CardHand(abilityPool, basicWalkCard, randomSeed != 0 ? new System.Random(randomSeed) : new System.Random());
            for (var i = 0; i < CardHand.SlotCount; i++) _slotIds[i] = $"card-{i}";

            movement.EncounterStarted += OnEncounterStarted;
            service.Selection.CanSelect = CanSelect;
            service.Selection.CardSelected += OnCardSelected;
            SetFocus(0); // Focus starts at zero every level
            Offer();
        }

        private void OnDestroy()
        {
            if (movement != null) movement.EncounterStarted -= OnEncounterStarted;
            if (service != null) service.Selection.CardSelected -= OnCardSelected;
        }

        // ------------------------------------------------------------------ offer

        private void Offer()
        {
            _slotById.Clear();
            var offer = new List<CardInfo>();
            for (var i = 0; i < CardHand.SlotCount; i++)
            {
                if (_usedSlots.Contains(i)) continue;
                _slotById[_slotIds[i]] = i;
                offer.Add(ToInfo(_slotIds[i], _hand.Slots[i]));
            }

            service.Selection.SetOffer(offer);
        }

        private static CardInfo ToInfo(string id, CardData card)
        {
            var faction = card.cardType != null && card.cardType.typeId != CardTypeId.Normal;
            var family = faction ? card.cardType.displayName : "Basic";
            var ability = card.ability != null ? card.ability.Label : card.effect.ToString();
            var color = card.cardType != null ? card.cardType.color : Color.gray;
            return new CardInfo(id, card.art, $"{family}\n<b>{card.cardName}</b>\n{ability}", color);
        }

        // ------------------------------------------------------------------ rules

        private bool CanSelect(CardInfo info)
        {
            if (!_slotById.TryGetValue(info.Id, out var slot)) return false;

            if (CardHand.IsWalkSlot(slot))
            {
                return movement.State == DiceBoardMovement.TurnState.AwaitingGo && movement.CurrentRoll == walkRoll && _focus < maxFocus;
            }

            return movement.State == DiceBoardMovement.TurnState.AwaitingEncounter && Use(_hand.Slots[slot], out _) != null;
        }

        /// <summary>
        /// What an Ability card does against the blocking enemy (null = can't be used now), and whether it needs Focus.
        /// </summary>
        private EncounterChoice? Use(CardData card, out bool byFocus)
        {
            byFocus = false;
            if (card.ability == null) return null;

            EncounterChoice? choice = null;
            if (card.ability.kind == AbilityKind.Damage) choice = EncounterChoice.Kill;
            else if (card.ability.kind == AbilityKind.Jump && movement.CanJumpPending) choice = EncounterChoice.Jump;
            if (choice == null) return null;

            if (card.cardType != null && card.cardType.luckyNumber == movement.CurrentRoll) return choice;
            if (_focus >= focusCost)
            {
                byFocus = true;
                return choice;
            }

            return null;
        }

        private void OnEncounterStarted(PathEncounter enemy)
        {
            for (var i = 0; i < CardHand.AbilitySlots; i++)
            {
                if (!_usedSlots.Contains(i) && Use(_hand.Slots[i], out _) != null) return; // the player chooses
            }

            movement.ResolveEncounter(EncounterChoice.TakeHit); // no usable card
        }

        private void OnCardSelected(CardInfo info)
        {
            if (!_slotById.TryGetValue(info.Id, out var slot)) return;

            if (CardHand.IsWalkSlot(slot))
            {
                SetFocus(_focus + 1);
            }
            else
            {
                var choice = Use(_hand.Slots[slot], out var byFocus);
                if (choice == null || !movement.ResolveEncounter(choice.Value)) return;
                if (byFocus) SetFocus(_focus - focusCost);
            }

            _usedSlots.Add(slot); // used up
        }

        private void SetFocus(int value)
        {
            _focus = Mathf.Clamp(value, 0, maxFocus);
            if (focusView != null) focusView.SetFocus(_focus);
        }
    }
}
