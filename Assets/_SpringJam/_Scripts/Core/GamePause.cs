using System;
using UnityEngine;

namespace SpringJam2026.Core
{
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }
        
        public static event Action<bool> OnPauseChanged;

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused)
                return;

            IsPaused = paused;

            OnPauseChanged?.Invoke(IsPaused);
        }
    }
}