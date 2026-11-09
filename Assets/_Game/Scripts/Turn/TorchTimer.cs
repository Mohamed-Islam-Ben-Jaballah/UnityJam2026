using System;

namespace DiceDrive.Turn
{
    public sealed class TorchTimer
    {
        public float Remaining { get; private set; }
        public float Duration { get; private set; }
        public bool IsRunning { get; private set; }
        public event Action Burnout;

        public TorchTimer(float duration)
        {
            Duration = duration;
            Remaining = duration;
        }

        public void Start()
        {
            IsRunning = true;
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Resume()
        {
            IsRunning = true;
        }

        public void Reset(float duration)
        {
            Duration = duration;
            Remaining = duration;
            IsRunning = false;
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsRunning || deltaSeconds <= 0f)
            {
                return;
            }

            Remaining -= deltaSeconds;
            if (Remaining > 0f)
            {
                return;
            }

            Remaining = 0f;
            IsRunning = false;
            Burnout?.Invoke();
        }
    }
}
