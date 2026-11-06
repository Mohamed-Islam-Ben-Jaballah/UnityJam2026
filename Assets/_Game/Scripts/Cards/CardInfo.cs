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

        public CardInfo(string id, Sprite artwork)
        {
            this.id = id;
            this.artwork = artwork;
        }

        public string Id => id;
        public Sprite Artwork => artwork;
    }
}
