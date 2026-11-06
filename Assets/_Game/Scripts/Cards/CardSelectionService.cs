using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>
    /// Scene access point for the card selection. Other systems reference this component and
    /// call <see cref="CardSelection.SetOffer"/> to show cards, or subscribe to
    /// <see cref="CardSelection.CardSelected"/> to react when the player picks one.
    /// </summary>
    public sealed class CardSelectionService : MonoBehaviour
    {
        [SerializeField] private bool logSelections = true;

        private CardSelection selection;

        // Lazy so subscribers in other objects' OnEnable work regardless of Awake order.
        public CardSelection Selection => selection ??= CreateSelection();

        private CardSelection CreateSelection()
        {
            var created = new CardSelection();
            created.CardSelected += card =>
            {
                if (logSelections) Debug.Log($"[CardSelection] Selected card '{card.Id}'", this);
            };
            return created;
        }
    }
}
