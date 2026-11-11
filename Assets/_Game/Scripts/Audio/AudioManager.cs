using UnityEngine;
using FMODUnity;
using FMOD.Studio;

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

    // For sounds the caller needs to control after starting (stop, fade, parameters).
    // The caller owns the instance: start() it, and release() it when done.
    public EventInstance CreateInstance(EventReference sound)
    {
        if (sound.IsNull)
        {
            Debug.LogWarning("AudioManager: EventReference is not assigned.");
            return default;
        }

        return RuntimeManager.CreateInstance(sound);
    }

    // Loads an event's audio into memory ahead of time so its first play doesn't stall.
    public void LoadSampleData(EventReference sound)
    {
        if (sound.IsNull) return;
        RuntimeManager.GetEventDescription(sound).loadSampleData();
    }
}
