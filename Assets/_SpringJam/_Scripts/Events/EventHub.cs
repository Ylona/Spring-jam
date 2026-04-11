using System;
using SpringJam.Systems.DayLoop;

namespace SpringJam2026.Events
{
    public static class EventHub
    {
        #region GameManagement
        
        public static event Action<bool> Ev_GamePaused;
        
        public static void Broadcast_GamePaused(bool isPaused) => Ev_GamePaused?.Invoke(isPaused);
        
        #endregion
    }   
}