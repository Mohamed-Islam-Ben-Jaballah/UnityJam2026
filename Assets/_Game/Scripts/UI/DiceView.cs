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
        [SerializeField] private float rollDuration = 3f;
        [SerializeField] private float spinSpeed = 900f;
        [Tooltip("Seconds between face changes at the start of the roll (fast).")]
        [UnityEngine.Serialization.FormerlySerializedAs("faceFlickerInterval")]
        [SerializeField] private float fastFaceInterval = 0.04f;
        [Tooltip("Seconds between face changes at the end of the roll (slow). The gap grows steadily from fast to slow.")]
        [SerializeField] private float slowFaceInterval = 0.45f;

        /// <summary>Raised when the player clicks the die while it is idle.</summary>
        public event Action RollRequested;

        /// <summary>Raised when a roll animation begins.</summary>
        public event Action RollStarted;

        /// <summary>Raised when the die has landed, with the value it shows.</summary>
        public event Action<int> Landed;

        public bool IsRolling { get; private set; }

        private Sprite pendingFace;
        private int pendingResult;
        private FMOD.Studio.EventInstance rollSound;

        private void Start()
        {
            // Load the roll sound up front so the first roll doesn't stall while FMOD reads it from disk.
            if (FMODEvents.instance != null && !FMODEvents.instance.diceRoll.IsNull)
                FMODUnity.RuntimeManager.GetEventDescription(FMODEvents.instance.diceRoll).loadSampleData();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !IsRolling) RollRequested?.Invoke();
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
            PlayRollSound();
            RollStarted?.Invoke();
            StartCoroutine(Roll(faces));
        }

        // Unity stops coroutines when the object is disabled; land immediately so the die
        // shows the result and never stays stuck in IsRolling.
        private void OnDisable()
        {
            if (!IsRolling) return;
            StopRollSound(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            Land();
        }

        private void PlayRollSound()
        {
            // The DiceRoll event allows one instance at a time and its tail outlasts the animation,
            // so a quick re-roll would be silenced: stop the previous sound before starting the new one.
            StopRollSound(FMOD.Studio.STOP_MODE.IMMEDIATE);
            if (FMODEvents.instance == null || FMODEvents.instance.diceRoll.IsNull) return;

            rollSound = FMODUnity.RuntimeManager.CreateInstance(FMODEvents.instance.diceRoll);
            rollSound.start();
            rollSound.release(); // freed by FMOD once it finishes playing
        }

        private void StopRollSound(FMOD.Studio.STOP_MODE mode)
        {
            if (rollSound.isValid()) rollSound.stop(mode);
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
            int face = Array.IndexOf(faces, dieGraphic.sprite);

            // Whole turns, so the spin ends exactly upright instead of snapping at the end.
            float totalSpin = Mathf.Max(1f, Mathf.Round(spinSpeed * rollDuration * 0.5f / 360f)) * 360f;

            for (float t = 0f; t < rollDuration; t += Time.unscaledDeltaTime)
            {
                float progress = t / rollDuration;
                float eased = 1f - (1f - progress) * (1f - progress); // fast start, slows to a stop
                graphic.localRotation = Quaternion.Euler(0f, 0f, -totalSpin * eased);
                graphic.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(progress * Mathf.PI));

                // The gap between face changes grows steadily: very fast first, slow at the end.
                flicker -= Time.unscaledDeltaTime;
                if (flicker <= 0f)
                {
                    face = NextFace(face, faces.Length);
                    dieGraphic.sprite = faces[face];
                    flicker = Mathf.Lerp(fastFaceInterval, slowFaceInterval, progress);
                }
                yield return null;
            }

            Land();
        }

        // A different face every time, so each change is visible even when it's slow.
        private static int NextFace(int current, int count)
        {
            if (count < 2) return 0;
            if (current < 0) return UnityEngine.Random.Range(0, count);
            int next = UnityEngine.Random.Range(0, count - 1);
            return next >= current ? next + 1 : next;
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
