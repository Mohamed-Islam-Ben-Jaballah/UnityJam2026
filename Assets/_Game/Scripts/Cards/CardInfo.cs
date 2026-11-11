using System;
using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>
    /// A card as delivered by the data source: a unique id plus its finished card image.
    /// Holds no UI or gameplay logic; any source (placeholder list, ScriptableObject, API) produces these.
    /// </summary>
    [Serializable]
    public sealed class CardInfo
    {
        [Tooltip("Unique, stable identifier. Selection is tracked by this value.")]
        [SerializeField] private string id;
        [Tooltip("The complete, finished card image.")]
        [SerializeField] private Sprite artwork;

        [Tooltip("Optional text shown on the card while it has no artwork (placeholder cards).")]
        [SerializeField] private string label;
        [Tooltip("Card colour while it has no artwork.")]
        [SerializeField] private Color placeholderColor = Color.gray;

        public CardInfo(string id, Sprite artwork)
        {
            this.id = id;
            this.artwork = artwork;
        }

        public CardInfo(string id, Sprite artwork, string label, Color placeholderColor) : this(id, artwork)
        {
            this.label = label;
            this.placeholderColor = placeholderColor;
        }

        public string Id => id;
        public Sprite Artwork => artwork;
        public string Label => label;
        public Color PlaceholderColor => placeholderColor;
    }
}
