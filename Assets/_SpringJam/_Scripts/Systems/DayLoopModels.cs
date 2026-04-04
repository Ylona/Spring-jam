using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpringJam.Systems.DayLoop
{
    [Serializable]
    public sealed class DayLoopTaskDefinition
    {
        [SerializeField] private string taskId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private bool startsUnlocked = true;
        [SerializeField] private bool requiredForCookingUnlock = true;

        public string TaskId => taskId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? taskId : displayName;
        public bool StartsUnlocked => startsUnlocked;
        public bool RequiredForCookingUnlock => requiredForCookingUnlock;

        public DayLoopTaskDefinition(string taskId, string displayName, bool startsUnlocked, bool requiredForCookingUnlock)
        {
            this.taskId = taskId ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.startsUnlocked = startsUnlocked;
            this.requiredForCookingUnlock = requiredForCookingUnlock;
        }
    }

    public sealed class DayLoopTaskSnapshot
    {
        public DayLoopTaskSnapshot(string taskId, string displayName, bool isUnlocked, bool isCompleted, bool requiredForCookingUnlock)
        {
            TaskId = taskId;
            DisplayName = displayName;
            IsUnlocked = isUnlocked;
            IsCompleted = isCompleted;
            RequiredForCookingUnlock = requiredForCookingUnlock;
        }

        public string TaskId { get; }
        public string DisplayName { get; }
        public bool IsUnlocked { get; }
        public bool IsCompleted { get; }
        public bool RequiredForCookingUnlock { get; }
    }

    public sealed class DayLoopSnapshot
    {
        public DayLoopSnapshot(
            int loopIndex,
            float elapsedSeconds,
            float dayDurationSeconds,
            IReadOnlyList<DayLoopTaskSnapshot> tasks,
            IReadOnlyCollection<string> learnedKnowledge)
        {
            LoopIndex = loopIndex;
            ElapsedSeconds = elapsedSeconds;
            DayDurationSeconds = dayDurationSeconds;
            Tasks = tasks;
            LearnedKnowledge = learnedKnowledge;
        }

        public int LoopIndex { get; }
        public float ElapsedSeconds { get; }
        public float DayDurationSeconds { get; }
        public float RemainingSeconds => Mathf.Max(0f, DayDurationSeconds - ElapsedSeconds);
        public float NormalizedProgress => DayDurationSeconds <= 0f ? 1f : Mathf.Clamp01(ElapsedSeconds / DayDurationSeconds);
        public IReadOnlyList<DayLoopTaskSnapshot> Tasks { get; }
        public IReadOnlyCollection<string> LearnedKnowledge { get; }
    }

    public enum DayLoopEndReason
    {
        TimeExpired,
        SuccessfulLoop,
        ManualReset,
    }

    public sealed class DayLoopEndContext
    {
        public DayLoopEndContext(DayLoopEndReason reason, DayLoopSnapshot endingSnapshot)
        {
            Reason = reason;
            EndingSnapshot = endingSnapshot;
        }

        public DayLoopEndReason Reason { get; }
        public DayLoopSnapshot EndingSnapshot { get; }
    }
}
