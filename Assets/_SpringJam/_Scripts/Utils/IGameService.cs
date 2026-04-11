using System;

namespace SpringJam2026.Utils
{
    public interface IGameService
    {
        int Priority { get; }
        void Initialize();  // Create/Setup
        void Bind();        // Subscribe to Events
    }
}