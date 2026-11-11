using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Fire-and-forget 2D sound. Variation is handled in FMOD Studio.
    public void PlayOneShot(EventReference sound)
    {
        if (sound.IsNull)
        {
            Debug.LogWarning("AudioManager: EventReference is not assigned.");
            return;
        }

        RuntimeManager.PlayOneShot(sound);
    }
}