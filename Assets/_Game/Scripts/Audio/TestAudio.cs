using UnityEngine;

public class TestAudio : MonoBehaviour
{
    [SerializeField] private float delay = 0.5f;   // seconds between plays

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= delay)
        {
            timer = 0f;
            AudioManager.instance.PlayOneShot(FMODEvents.instance.elfCharm);
        }
    }
}