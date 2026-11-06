using System.Collections.Generic;
using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>
    /// Temporary data source: offers the cards listed in the Inspector when the scene starts.
    /// Replace or remove it once real card data exists — anything that calls
    /// <c>service.Selection.SetOffer(cards)</c> works the same way.
    /// </summary>
    public sealed class PlaceholderCardSource : MonoBehaviour
    {
        [SerializeField] private CardSelectionService service;
        [SerializeField] private List<CardInfo> cards = new();

        private void Start() => Offer();

        [ContextMenu("Offer Cards Again")]
        private void Offer() => service.Selection.SetOffer(cards);
    }
}
