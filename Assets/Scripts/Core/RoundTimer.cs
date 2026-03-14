using System;

namespace MatchThree.Core
{
    public class RoundTimer
    {
        public float Duration { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool IsExpired => TimeRemaining <= 0f;

        private bool _hasFired;

        public event Action OnExpired;

        public RoundTimer(float duration)
        {
            Duration = duration;
            TimeRemaining = duration;
        }

        public void Tick(float deltaTime)
        {
            if (_hasFired) return;

            TimeRemaining -= deltaTime;
            if (TimeRemaining < 0f) TimeRemaining = 0f;

            if (IsExpired && !_hasFired)
            {
                _hasFired = true;
                OnExpired?.Invoke();
            }
        }

        public void Reset()
        {
            TimeRemaining = Duration;
            _hasFired = false;
        }
    }
}
