using System;
using UnityEngine;

namespace DiceDrive.UI
{
    /// <summary>
    /// Fan layout for any number of cards: cards sit on a shallow arc, and spacing shrinks
    /// once the hand would exceed <see cref="maxWidth"/>. Pure math; tweak values in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class HandLayout
    {
        [Tooltip("Preferred distance between card centers.")]
        public float cardSpacing = 230f;
        [Tooltip("Hand never gets wider than this; spacing compresses instead.")]
        public float maxWidth = 1150f;
        [Tooltip("Rotation step between neighbouring cards (degrees).")]
        public float anglePerCard = 4f;
        [Tooltip("Outermost card never rotates more than this (degrees).")]
        public float maxAngle = 14f;

        [Header("Hover")]
        [Tooltip("How far the hovered card rises. With the hand on the screen edge, keep the lifted card's bottom below the edge so the pointer never falls off it.")]
        public float hoverLift = 190f;
        public float hoverScale = 1.2f;
        [Tooltip("Neighbours slide away from the hovered card by this much.")]
        public float hoverPush = 45f;
        public float dimmedScale = 0.94f;
        [Tooltip("How strongly non-hovered cards are shaded while another card is hovered (0-1).")]
        [Range(0f, 1f)] public float shadeAmount = 1f;

        public void GetSlot(int index, int count, int hoveredIndex, out Vector2 position, out float angle)
        {
            float half = (count - 1) * 0.5f;
            float offset = index - half;
            float spacing = count > 1 ? Mathf.Min(cardSpacing, maxWidth / (count - 1)) : 0f;
            float step = half > 0f ? Mathf.Min(anglePerCard, maxAngle / half) : 0f;

            angle = -offset * step;

            // Place cards on a circle whose curvature matches the rotation step.
            float stepRad = Mathf.Max(step, 0.01f) * Mathf.Deg2Rad;
            float radius = spacing / stepRad;
            float y = -(1f - Mathf.Cos(offset * stepRad)) * radius;
            float x = offset * spacing;

            if (hoveredIndex >= 0 && index != hoveredIndex)
                x += Mathf.Sign(index - hoveredIndex) * hoverPush;

            position = new Vector2(x, y);
        }
    }
}
