using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DiceDrive.Cinematics
{
    /// <summary>
    /// Cinematic opening for Level 1: black screen -> narrative line fades in -> characters
    /// evaporate upward while the black backdrop dissolves to reveal the level -> gameplay unlocks.
    ///
    /// The overlay is opaque from the very first frame (enforced in Awake, which runs before any
    /// other gameplay script thanks to DefaultExecutionOrder), so the level is never exposed
    /// uninitialised. Input is blocked by a top-most raycast-blocking canvas, by disabling any
    /// EventSystems, and by keeping the behaviours listed in <see cref="disableDuringIntro"/>
    /// switched off until the sequence ends.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class OpeningCutsceneController : MonoBehaviour
    {
        public enum PlayPolicy
        {
            /// <summary>Plays on the first load of this scene in a session (or after RequestPlayOnNextLoad).</summary>
            OncePerSession,
            /// <summary>Plays every time the scene loads.</summary>
            Always,
            /// <summary>Never plays; gameplay starts immediately.</summary>
            Never
        }

        /// <summary>True while any opening cutscene is running. Gameplay code can poll this.</summary>
        public static bool IsPlaying { get; private set; }

        /// <summary>Raised once gameplay has been handed back to the player.</summary>
        public static event Action IntroFinished;

        private static bool s_playedThisSession;
        private static bool s_forcePlayNextLoad;

        [Header("References")]
        [Tooltip("Root of the cutscene UI. Disabled once the intro has finished.")]
        [SerializeField] private GameObject overlayRoot;
        [Tooltip("CanvasGroup on the full-screen black image. Its alpha drives the reveal.")]
        [SerializeField] private CanvasGroup backdropGroup;
        [Tooltip("CanvasGroup on the narrative text. Drives fade-in and skip fade-out.")]
        [SerializeField] private CanvasGroup textGroup;
        [SerializeField] private TextMeshProUGUI narrativeText;

        [Header("Content")]
        [TextArea(2, 5)]
        [SerializeField] private string openingLine =
            "Some promises are stronger than fate.\nOthers are the reason it breaks.";
        [Tooltip("Optional override. Leave empty to keep the font assigned on the text component.")]
        [SerializeField] private TMP_FontAsset fontOverride;

        [Header("Playback")]
        [SerializeField] private PlayPolicy playPolicy = PlayPolicy.OncePerSession;

        [Header("Timing (seconds, unscaled)")]
        [Min(0f)] [SerializeField] private float initialBlackDelay = 0.5f;
        [Min(0.01f)] [SerializeField] private float textFadeInDuration = 1.8f;
        [Min(0f)] [SerializeField] private float holdDuration = 3f;
        [Tooltip("Delay between the start of the evaporation and the start of the backdrop fade.")]
        [Min(0f)] [SerializeField] private float backdropFadeDelay = 0.4f;
        [Min(0.01f)] [SerializeField] private float backdropFadeDuration = 3f;

        [Header("Fade In")]
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Text rises this many canvas units while fading in (0 = static).")]
        [SerializeField] private float fadeInRise = 10f;

        [Header("Evaporation")]
        [Tooltip("Time each character takes to fully dissolve.")]
        [Min(0.01f)] [SerializeField] private float charEvaporateDuration = 1.4f;
        [Tooltip("Time between the first and last character starting to dissolve (left-to-right wave).")]
        [Min(0f)] [SerializeField] private float staggerSpread = 1.2f;
        [Tooltip("Extra random delay per character so the wave feels organic.")]
        [Min(0f)] [SerializeField] private float staggerRandomness = 0.5f;
        [Tooltip("Upward drift of each character, in canvas units.")]
        [SerializeField] private float driftDistance = 38f;
        [Tooltip("Sideways wisp of each character, in canvas units.")]
        [SerializeField] private float swayAmount = 6f;
        [Tooltip("Extra scale each character gains while dissolving (0.15 = +15%).")]
        [SerializeField] private float scaleGrowth = 0.15f;
        [Tooltip("Small tilt each character gains while dissolving, in degrees.")]
        [SerializeField] private float maxRotation = 6f;
        [SerializeField] private AnimationCurve evaporateCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private int randomSeed = 1337;

        [Header("Background Reveal")]
        [SerializeField] private AnimationCurve backdropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Optional orthographic camera that gently settles into place during the reveal.")]
        [SerializeField] private Camera revealCamera;
        [Tooltip("Orthographic size multiplier at the start of the reveal (1 = no camera move).")]
        [Range(1f, 1.3f)] [SerializeField] private float cameraStartZoom = 1.06f;

        [Header("Skip")]
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private Key[] skipKeys = { Key.Space, Key.Escape };
        [Tooltip("Ignore skip input for this long after the intro starts (avoids accidental skips).")]
        [Min(0f)] [SerializeField] private float skipGracePeriod = 0.25f;
        [Tooltip("Duration of the quick, eased fade used when skipping. Never a hard cut.")]
        [Min(0.01f)] [SerializeField] private float skipFadeDuration = 0.45f;

        [Header("Gameplay Hand-off")]
        [Tooltip("Gameplay behaviours (controllers, input readers, bootstraps) kept disabled until the intro ends.")]
        [SerializeField] private List<Behaviour> disableDuringIntro = new List<Behaviour>();
        [Tooltip("GameObjects (HUD, gameplay roots) activated when the intro ends.")]
        [SerializeField] private List<GameObject> activateAfterIntro = new List<GameObject>();
        [Tooltip("Disable every EventSystem in the scene while the intro plays.")]
        [SerializeField] private bool disableEventSystemsDuringIntro = true;
        [SerializeField] private UnityEvent onIntroStarted = new UnityEvent();
        [SerializeField] private UnityEvent onIntroFinished = new UnityEvent();

        private readonly List<Behaviour> _behavioursToRestore = new List<Behaviour>();
        private readonly List<EventSystem> _eventSystemsToRestore = new List<EventSystem>();
        private Coroutine _sequence;
        private bool _running;
        private bool _finished;
        private bool _skipping;
        private float _startTime;
        private float _cameraBaseSize;
        private Vector2 _textBasePosition;

        // Evaporation mesh cache.
        private Vector3[][] _sourceVertices;
        private Color32[][] _sourceColors;
        private float[] _charDelays;
        private float[] _charSeeds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Keeps "once per session" correct when Enter Play Mode Options skip domain reload.
            IsPlaying = false;
            IntroFinished = null;
            s_playedThisSession = false;
            s_forcePlayNextLoad = false;
        }

        /// <summary>
        /// Call from a "New Game" button before loading Level 1 to guarantee the intro plays,
        /// even if it already played earlier in this session.
        /// </summary>
        public static void RequestPlayOnNextLoad() => s_forcePlayNextLoad = true;

        private void Awake()
        {
            if (revealCamera != null && revealCamera.orthographic)
            {
                _cameraBaseSize = revealCamera.orthographicSize;
            }

            if (narrativeText != null)
            {
                _textBasePosition = narrativeText.rectTransform.anchoredPosition;
            }

            if (!ShouldPlay())
            {
                CompleteImmediately(raiseEvents: true);
                return;
            }

            BeginIntroState();
        }

        private void Start()
        {
            if (_running && _sequence == null)
            {
                _sequence = StartCoroutine(PlaySequence());
            }
        }

        private void Update()
        {
            if (!_running || _skipping || !allowSkip)
            {
                return;
            }

            if (Time.unscaledTime - _startTime < skipGracePeriod)
            {
                return;
            }

            if (SkipPressed())
            {
                Skip();
            }
        }

        private void OnDestroy()
        {
            // Scene unloaded mid-intro: never leave the global flag stuck on.
            if (_running)
            {
                IsPlaying = false;
            }
        }

        /// <summary>Skips the intro with a short eased fade and hands control to the player.</summary>
        public void Skip()
        {
            if (!_running || _skipping)
            {
                return;
            }

            _skipping = true;
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
            }

            _sequence = StartCoroutine(SkipSequence());
        }

        [ContextMenu("Replay Intro (Play Mode)")]
        private void ReplayFromContextMenu()
        {
            if (!Application.isPlaying || _running)
            {
                return;
            }

            _finished = false;
            _skipping = false;
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
            }

            BeginIntroState();
            _sequence = StartCoroutine(PlaySequence());
        }

        private bool ShouldPlay()
        {
            if (s_forcePlayNextLoad)
            {
                return true;
            }

            switch (playPolicy)
            {
                case PlayPolicy.Always: return true;
                case PlayPolicy.Never: return false;
                default: return !s_playedThisSession;
            }
        }

        private void BeginIntroState()
        {
            s_forcePlayNextLoad = false;
            s_playedThisSession = true;
            _running = true;
            IsPlaying = true;
            _startTime = Time.unscaledTime;

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
            }

            if (backdropGroup != null)
            {
                backdropGroup.alpha = 1f;
                backdropGroup.blocksRaycasts = true;
                backdropGroup.interactable = false;
            }

            if (textGroup != null)
            {
                textGroup.alpha = 0f;
                textGroup.blocksRaycasts = false;
            }

            if (narrativeText != null)
            {
                if (fontOverride != null)
                {
                    narrativeText.font = fontOverride;
                }

                narrativeText.text = openingLine;
                narrativeText.rectTransform.anchoredPosition = _textBasePosition;
                narrativeText.gameObject.SetActive(true);
            }

            if (revealCamera != null && revealCamera.orthographic)
            {
                revealCamera.orthographicSize = _cameraBaseSize * cameraStartZoom;
            }

            LockGameplay();
            onIntroStarted.Invoke();
        }

        private IEnumerator PlaySequence()
        {
            // 1. Pure black.
            yield return WaitUnscaled(initialBlackDelay);

            // 2. Fade the line in with a gentle rise.
            yield return Animate(textFadeInDuration, t =>
            {
                float e = fadeInCurve.Evaluate(t);
                textGroup.alpha = e;
                narrativeText.rectTransform.anchoredPosition =
                    _textBasePosition + Vector2.down * (fadeInRise * (1f - e));
            });

            // 3. Hold.
            yield return WaitUnscaled(holdDuration);

            // 4 + 5. Evaporate characters while the backdrop dissolves.
            PrepareEvaporation();
            float evaporationTotal = staggerSpread + staggerRandomness + charEvaporateDuration;
            float revealEnd = backdropFadeDelay + backdropFadeDuration;
            float total = Mathf.Max(evaporationTotal, revealEnd);
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                ApplyEvaporation(elapsed);

                float r = Mathf.Clamp01((elapsed - backdropFadeDelay) / backdropFadeDuration);
                ApplyReveal(backdropCurve.Evaluate(r));
                yield return null;
            }

            Finish();
        }

        private IEnumerator SkipSequence()
        {
            float startBackdrop = backdropGroup != null ? backdropGroup.alpha : 0f;
            float startText = textGroup != null ? textGroup.alpha : 0f;
            float startReveal = 1f - startBackdrop;

            yield return Animate(skipFadeDuration, t =>
            {
                float e = Smooth(t);
                if (textGroup != null) textGroup.alpha = Mathf.Lerp(startText, 0f, e);
                ApplyReveal(Mathf.Lerp(startReveal, 1f, e));
            });

            Finish();
        }

        /// <param name="reveal">0 = fully black, 1 = level fully visible.</param>
        private void ApplyReveal(float reveal)
        {
            if (backdropGroup != null)
            {
                backdropGroup.alpha = 1f - reveal;
            }

            if (revealCamera != null && revealCamera.orthographic)
            {
                revealCamera.orthographicSize =
                    Mathf.Lerp(_cameraBaseSize * cameraStartZoom, _cameraBaseSize, reveal);
            }
        }

        private void Finish()
        {
            if (_finished)
            {
                return;
            }

            _sequence = null;
            CompleteImmediately(raiseEvents: true);
        }

        private void CompleteImmediately(bool raiseEvents)
        {
            _finished = true;
            _running = false;
            _skipping = false;
            IsPlaying = false;

            if (backdropGroup != null)
            {
                backdropGroup.alpha = 0f;
                backdropGroup.blocksRaycasts = false;
            }

            if (textGroup != null)
            {
                textGroup.alpha = 0f;
            }

            if (revealCamera != null && revealCamera.orthographic && _cameraBaseSize > 0f)
            {
                revealCamera.orthographicSize = _cameraBaseSize;
            }

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            UnlockGameplay();

            if (raiseEvents)
            {
                onIntroFinished.Invoke();
                IntroFinished?.Invoke();
            }
        }

        // ---------------------------------------------------------------- Gameplay lock

        private void LockGameplay()
        {
            _behavioursToRestore.Clear();
            foreach (Behaviour behaviour in disableDuringIntro)
            {
                if (behaviour != null && behaviour.enabled && behaviour != this)
                {
                    behaviour.enabled = false;
                    _behavioursToRestore.Add(behaviour);
                }
            }

            foreach (GameObject go in activateAfterIntro)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }

            _eventSystemsToRestore.Clear();
            if (disableEventSystemsDuringIntro)
            {
                foreach (EventSystem es in FindObjectsByType<EventSystem>())
                {
                    if (es.enabled)
                    {
                        es.enabled = false;
                        _eventSystemsToRestore.Add(es);
                    }
                }
            }
        }

        private void UnlockGameplay()
        {
            foreach (Behaviour behaviour in _behavioursToRestore)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            _behavioursToRestore.Clear();

            foreach (GameObject go in activateAfterIntro)
            {
                if (go != null)
                {
                    go.SetActive(true);
                }
            }

            foreach (EventSystem es in _eventSystemsToRestore)
            {
                if (es != null)
                {
                    es.enabled = true;
                }
            }

            _eventSystemsToRestore.Clear();
        }

        // ---------------------------------------------------------------- Evaporation

        private void PrepareEvaporation()
        {
            narrativeText.ForceMeshUpdate();
            TMP_TextInfo info = narrativeText.textInfo;

            _sourceVertices = new Vector3[info.meshInfo.Length][];
            _sourceColors = new Color32[info.meshInfo.Length][];
            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                _sourceVertices[m] = (Vector3[])info.meshInfo[m].vertices.Clone();
                _sourceColors[m] = (Color32[])info.meshInfo[m].colors32.Clone();
            }

            int count = info.characterCount;
            _charDelays = new float[count];
            _charSeeds = new float[count];
            var rng = new System.Random(randomSeed);
            for (int i = 0; i < count; i++)
            {
                float order = count > 1 ? (float)i / (count - 1) : 0f;
                _charDelays[i] = order * staggerSpread + (float)rng.NextDouble() * staggerRandomness;
                _charSeeds[i] = (float)rng.NextDouble() * 2f - 1f;
            }
        }

        private void ApplyEvaporation(float elapsed)
        {
            TMP_TextInfo info = narrativeText.textInfo;
            if (_sourceVertices == null || info.meshInfo.Length != _sourceVertices.Length)
            {
                return;
            }

            for (int i = 0; i < info.characterCount && i < _charDelays.Length; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (!ch.isVisible)
                {
                    continue;
                }

                float p = Mathf.Clamp01((elapsed - _charDelays[i]) / charEvaporateDuration);
                float e = evaporateCurve.Evaluate(p);
                float seed = _charSeeds[i];

                int m = ch.materialReferenceIndex;
                int v = ch.vertexIndex;
                Vector3[] src = _sourceVertices[m];
                Color32[] srcCol = _sourceColors[m];
                Vector3[] dst = info.meshInfo[m].vertices;
                Color32[] dstCol = info.meshInfo[m].colors32;

                Vector3 center = (src[v] + src[v + 2]) * 0.5f;
                Vector3 offset = new Vector3(
                    Mathf.Sin(e * Mathf.PI * (1f + Mathf.Abs(seed))) * swayAmount * seed,
                    driftDistance * e * (0.8f + 0.4f * Mathf.Abs(seed)),
                    0f);
                Matrix4x4 xf = Matrix4x4.TRS(
                    center + offset,
                    Quaternion.Euler(0f, 0f, maxRotation * seed * e),
                    Vector3.one * (1f + scaleGrowth * e));

                // Alpha falls slightly ahead of the motion so letters thin out as they rise.
                float alpha = 1f - Mathf.Clamp01(e * 1.15f);

                for (int k = 0; k < 4; k++)
                {
                    dst[v + k] = xf.MultiplyPoint3x4(src[v + k] - center);
                    Color32 c = srcCol[v + k];
                    c.a = (byte)(c.a * alpha);
                    dstCol[v + k] = c;
                }
            }

            narrativeText.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        // ---------------------------------------------------------------- Helpers

        private bool SkipPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || skipKeys == null)
            {
                return false;
            }

            foreach (Key key in skipKeys)
            {
                if (key != Key.None && keyboard[key].wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                yield return null;
            }
        }

        private static IEnumerator Animate(float duration, Action<float> step)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            step(1f);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
