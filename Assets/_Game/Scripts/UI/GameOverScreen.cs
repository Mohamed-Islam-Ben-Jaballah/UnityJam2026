using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceDrive.UI
{
    /// <summary>
    /// Game over screen, shown in its own scene (GameOver.unity) when the run ends (player HP 0).
    ///
    /// Gameplay connects it with one line, once the session exists:
    ///     session.RunEnded += GameOverScreen.Show;
    ///
    /// Show() remembers the scene the player died in, then loads the GameOver scene.
    /// "Rise Again" reloads the scene the player died in; "Quit" exits.
    /// </summary>
    public sealed class GameOverScreen : MonoBehaviour
    {
        public const string SceneName = "GameOver";

        // Carried from the gameplay scene into the GameOver scene.
        private static string s_returnScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_returnScene = null;

        /// <summary>Remembers the current scene and loads the GameOver scene.</summary>
        public static void Show()
        {
            s_returnScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(SceneName);
        }

        [Header("References")]
        [Tooltip("Full-screen black layer that fades out to reveal the screen.")]
        [SerializeField] private CanvasGroup fadeFromBlack;
        [Tooltip("Title and subtitle. Fades in and settles from a slight zoom.")]
        [SerializeField] private CanvasGroup content;
        [Tooltip("Buttons. Fade in last and are not clickable until visible.")]
        [SerializeField] private CanvasGroup buttons;
        [Tooltip("Ember glow behind the title. Its alpha flickers like a dying torch.")]
        [SerializeField] private Graphic emberGlow;
        [SerializeField] private Button riseAgainButton;
        [SerializeField] private Button quitButton;

        [Header("Timing (seconds, unscaled)")]
        [Min(0.01f)] [SerializeField] private float blackFadeDuration = 1.2f;
        [Min(0.01f)] [SerializeField] private float contentFadeDuration = 1.6f;
        [Min(0f)] [SerializeField] private float buttonsDelay = 0.6f;
        [Min(0.01f)] [SerializeField] private float buttonsFadeDuration = 0.8f;
        [Tooltip("Content starts this much larger and settles to 1.")]
        [SerializeField] private float startScale = 1.08f;

        [Header("Ember Flicker")]
        [SerializeField] private float emberMinAlpha = 0.25f;
        [SerializeField] private float emberMaxAlpha = 0.55f;
        [SerializeField] private float emberFlickerSpeed = 1.3f;

        private void Awake()
        {
            // Gameplay may have paused time at the moment of death; the next scene should run normally.
            Time.timeScale = 1f;

            fadeFromBlack.alpha = 1f;
            fadeFromBlack.blocksRaycasts = true;
            content.alpha = 0f;
            buttons.alpha = 0f;
            buttons.interactable = false;

            riseAgainButton.onClick.AddListener(RiseAgain);
            quitButton.onClick.AddListener(Quit);
            // Nowhere to return to when the scene is opened directly.
            riseAgainButton.gameObject.SetActive(!string.IsNullOrEmpty(s_returnScene));
        }

        private void Start()
        {
            StartCoroutine(Reveal());
        }

        private void Update()
        {
            if (emberGlow == null) return;
            float n = Mathf.PerlinNoise(Time.unscaledTime * emberFlickerSpeed, 0.37f);
            var c = emberGlow.color;
            c.a = Mathf.Lerp(emberMinAlpha, emberMaxAlpha, n);
            emberGlow.color = c;
        }

        private IEnumerator Reveal()
        {
            var contentRect = (RectTransform)content.transform;
            yield return Fade(blackFadeDuration, t => fadeFromBlack.alpha = 1f - t);
            fadeFromBlack.blocksRaycasts = false;

            yield return Fade(contentFadeDuration, t =>
            {
                content.alpha = t;
                contentRect.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, t);
            });

            yield return new WaitForSecondsRealtime(buttonsDelay);
            yield return Fade(buttonsFadeDuration, t => buttons.alpha = t);
            buttons.interactable = true;
        }

        private static IEnumerator Fade(float duration, System.Action<float> apply)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                apply(Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            apply(1f);
        }

        private void RiseAgain()
        {
            buttons.interactable = false;
            SceneManager.LoadScene(s_returnScene);
        }

        private void Quit()
        {
            buttons.interactable = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
