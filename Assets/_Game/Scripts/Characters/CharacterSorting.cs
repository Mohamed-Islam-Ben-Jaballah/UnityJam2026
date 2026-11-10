using UnityEngine;
using UnityEngine.Rendering;

namespace DiceDrive.Characters
{
    /// <summary>
    /// Keeps a character (player or enemy) on the dedicated top-most "Characters" sorting layer, so no
    /// environment layer (Foreground, Decoration, VFX...) can ever be drawn over it, regardless of order numbers.
    /// The whole character renders as one unit through a <see cref="SortingGroup"/>; within that layer,
    /// characters lower on screen are drawn in front of characters higher up.
    /// <see cref="CharacterActor"/> adds this automatically, so every board character gets it.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CharacterSorting : MonoBehaviour
    {
        public const string DefaultLayerName = "Characters";

        [Tooltip("Sorting layer for characters. Must be the last (top-most) layer in Project Settings > Tags and Layers.")]
        [SerializeField] private string sortingLayerName = DefaultLayerName;

        [Tooltip("Base order inside the Characters layer.")]
        [SerializeField] private int baseOrder;

        [Tooltip("Characters lower on screen draw in front of those higher up.")]
        [SerializeField] private bool sortByY = true;

        [Tooltip("Order steps per world unit of Y when sorting by Y.")]
        [SerializeField, Min(1)] private int yPrecision = 100;

        private SortingGroup _group;
        private static bool s_warnedMissingLayer;

        private void OnEnable()
        {
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (_group == null && !TryGetComponent(out _group))
            {
                _group = gameObject.AddComponent<SortingGroup>();
            }

            int layerId = SortingLayer.NameToID(sortingLayerName);
            if (!SortingLayer.IsValid(layerId))
            {
                if (!s_warnedMissingLayer)
                {
                    s_warnedMissingLayer = true;
                    Debug.LogWarning($"[CharacterSorting] Sorting layer '{sortingLayerName}' does not exist. " +
                                     "Add it as the last layer in Project Settings > Tags and Layers > Sorting Layers.", this);
                }

                return;
            }

            int order = baseOrder;
            if (sortByY)
            {
                order -= Mathf.RoundToInt(transform.position.y * yPrecision);
            }

            order = Mathf.Clamp(order, short.MinValue, short.MaxValue);

            if (_group.sortingLayerID != layerId)
            {
                _group.sortingLayerID = layerId;
            }

            if (_group.sortingOrder != order)
            {
                _group.sortingOrder = order;
            }
        }
    }
}
