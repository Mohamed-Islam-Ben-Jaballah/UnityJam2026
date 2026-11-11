using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>
    /// Holds the scene's <see cref="CardSelection"/> (the hand's UI state) for the card UI components.
    /// Gameplay code should talk to <c>CardUIGateway</c> instead of using this directly.
    /// </summary>
    public sealed class CardSelectionService : MonoBehaviour
    {
        [SerializeField] private bool logSelections;

        private CardSelection selection;

        // Lazy so subscribers in other objects' OnEnable work regardless of Awake order.
        public CardSelection Selection => selection ??= CreateSelection();

        private CardSelection CreateSelection()
        {
            var created = new CardSelection();
            created.CardSelected += LogSelection;
            return created;
        }

        private void LogSelection(CardInfo card)
        {
            if (logSelections) Debug.Log($"[CardSelection] Selected card '{card.Id}'", this);
        }
    }
}
