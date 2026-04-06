using System;

namespace Game.Core
{
    public static class GameEvents
    {
        // Parameter 1: Duration, Parameter 2: Intensity (0 to 1)
        public static event Action<float, float> OnRequestScreenFlash;

        public static void RequestScreenFlash(float duration, float intensity)
        {
            OnRequestScreenFlash?.Invoke(duration, intensity);
        }
    }
}