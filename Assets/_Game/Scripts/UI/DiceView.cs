using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDrive.UI
{
    /// <summary>
    /// Clickable die. Reports roll requests and animates a result it is given; it never decides
    /// the result itself, so any roller (placeholder or GameSession) can drive it.
    /// Face sprites are looked up by the die's number of sides.
    /// </summary>
    public sealed class DiceView : MonoBehaviour, IPointerClickHandler
    {
        [Serializable]
        private sealed class FaceSet
        {
            public int sides;
            [Tooltip("Face sprites in order: element 0 shows 1, element 1 shows 2, ...")]
            public Sprite[] faces;
        }

        [SerializeField] private Image dieGraphic;
        [SerializeField] private FaceSet[] faceSets;
        [SerializeField] private float rollDuration = 0.7f;
        [SerializeField] private float spinSpeed = 900f;
        [SerializeField] private float faceFlickerInterval = 0.06f;

        /// <summary>Raised when the player clicks the die while it is idle.</summary>
        public event Action RollRequested;

        /// <summary>Raised when a roll animation begins.</summary>
        public event Action RollStarted;

        /// <summary>Raised when the die has landed, with the value it shows.</summary>
        public event Action<int> Landed;

        public bool IsRolling { get; private set; }

        [Tooltip("Tint of the die while it can't be clicked (e.g. while the player is walking).")]
        [SerializeField] private Color disabledTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        private bool interactable = true;

        /// <summary>When false, clicks are ignored and the die is dimmed.</summary>
        public bool Interactable
        {
            get => interactable;
            set
            {
                interactable = value;
                if (dieGraphic != null) dieGraphic.color = value ? Color.white : disabledTint;
            }
        }

        private Sprite pendingFace;
        private int pendingResult;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !IsRolling && interactable) RollRequested?.Invoke();
        }

        /// <summary>Plays the tumble animation and lands on <paramref name="result"/>.</summary>
        public void PlayRoll(int result, int sides)
        {
            var faces = FacesFor(sides);
            if (faces == null || result < 1 || result > faces.Length)
            {
                Debug.LogError($"[DiceView] No face sprite for {result} on a d{sides}.", this);
                return;
            }

            StopAllCoroutines();
            pendingFace = faces[result - 1];
            pendingResult = result;
            IsRolling = true;
            RollStarted?.Invoke();
            StartCoroutine(Roll(faces));
        }

        // Unity stops coroutines when the object is disabled; land immediately so the die
        // shows the result and never stays stuck in IsRolling.
        private void OnDisable()
        {
            if (IsRolling) Land();
        }

        private Sprite[] FacesFor(int sides)
        {
            foreach (var set in faceSets)
            {
                if (set.sides == sides) return set.faces;
            }
            return null;
        }

        private IEnumerator Roll(Sprite[] faces)
        {
            var graphic = dieGraphic.rectTransform;
            float flicker = 0f;

            for (float t = 0f; t < rollDuration; t += Time.unscaledDeltaTime)
            {
                float progress = t / rollDuration;
                float speed = spinSpeed * (1f - progress); // slows down as it settles
                graphic.Rotate(0f, 0f, -speed * Time.unscaledDeltaTime);
                graphic.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(progress * Mathf.PI));

                flicker -= Time.unscaledDeltaTime;
                if (flicker <= 0f)
                {
                    dieGraphic.sprite = faces[UnityEngine.Random.Range(0, faces.Length)];
                    flicker = faceFlickerInterval;
                }
                yield return null;
            }

            Land();
        }

        private void Land()
        {
            var graphic = dieGraphic.rectTransform;
            graphic.localRotation = Quaternion.identity;
            graphic.localScale = Vector3.one;
            dieGraphic.sprite = pendingFace;
            IsRolling = false;
            Landed?.Invoke(pendingResult);
        }
    }
}
