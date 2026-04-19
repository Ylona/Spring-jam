using System;
using System.Collections.Generic;

namespace SpringJam.Systems.DayLoop
{
    public sealed class DayLoopStateMachine
    {
        public event Action<DayLoopSnapshot> LoopStarted;
        public event Action<DayLoopSnapshot> PhaseChanged;
        public event Action<DayLoopEndContext> LoopEnded;
        public event Action<DayLoopTaskSnapshot> TaskChanged;
        public event Action<string> KnowledgeLearned;

        private readonly float dayDurationSeconds;
        private readonly float startDayDurationSeconds;
        private readonly List<RuntimeTaskState> runtimeTasks;
        private readonly Dictionary<string, RuntimeTaskState> runtimeTaskLookup;
        private readonly HashSet<string> learnedKnowledge;

        private bool hasBegun;
        private bool isFrozen;
        private float elapsedSeconds;
        private float phaseElapsedSeconds;
        private DayLoopPhase currentPhase;

        public DayLoopStateMachine(float dayDurationSeconds, IEnumerable<DayLoopTaskDefinition> taskDefinitions)
            : this(dayDurationSeconds, 0f, taskDefinitions)
        {
        }

        public DayLoopStateMachine(float dayDurationSeconds, float startDayDurationSeconds, IEnumerable<DayLoopTaskDefinition> taskDefinitions)
        {
            if (dayDurationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(dayDurationSeconds), "Day duration must be greater than zero.");
            }

            if (startDayDurationSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startDayDurationSeconds), "Start day duration cannot be negative.");
            }

            this.dayDurationSeconds = dayDurationSeconds;
            this.startDayDurationSeconds = startDayDurationSeconds;
            runtimeTasks = BuildRuntimeTasks(taskDefinitions);
            runtimeTaskLookup = new Dictionary<string, RuntimeTaskState>(runtimeTasks.Count, StringComparer.Ordinal);
            learnedKnowledge = new HashSet<string>(StringComparer.Ordinal);

            foreach (RuntimeTaskState task in runtimeTasks)
            {
                runtimeTaskLookup.Add(task.TaskId, task);
            }
        }

        public int ActiveLoopIndex { get; private set; }
        public float DayDurationSeconds => dayDurationSeconds;
        public float StartDayDurationSeconds => startDayDurationSeconds;
        public float ElapsedSeconds => elapsedSeconds;
        public float PhaseElapsedSeconds => phaseElapsedSeconds;
        public DayLoopPhase CurrentPhase => currentPhase;
        public DayLoopSnapshot CurrentSnapshot => hasBegun ? CreateSnapshot() : null;

        public void Begin()
        {
            if (hasBegun)
            {
                return;
            }

            StartNextLoop();
        }

        public void Freeze()
        {
            isFrozen = true;
        }

        public void Tick(float deltaTime)
        {
            if (!hasBegun || isFrozen || deltaTime <= 0f)
            {
                return;
            }

            float remainingDelta = deltaTime;
            while (remainingDelta > 0f)
            {
                if (currentPhase == DayLoopPhase.StartDay)
                {
                    if (startDayDurationSeconds <= 0f)
                    {
                        StartActiveDay();
                        continue;
                    }

                    float remainingStartDaySeconds = startDayDurationSeconds - phaseElapsedSeconds;
                    if (remainingDelta < remainingStartDaySeconds)
                    {
                        phaseElapsedSeconds += remainingDelta;
                        return;
                    }

                    phaseElapsedSeconds = startDayDurationSeconds;
                    remainingDelta -= remainingStartDaySeconds;
                    StartActiveDay();
                    continue;
                }

                float remainingDaySeconds = dayDurationSeconds - elapsedSeconds;
                if (remainingDelta < remainingDaySeconds)
                {
                    elapsedSeconds += remainingDelta;
                    phaseElapsedSeconds += remainingDelta;
                    return;
                }

                elapsedSeconds = dayDurationSeconds;
                phaseElapsedSeconds = dayDurationSeconds;
                EndLoop(DayLoopEndReason.TimeExpired);
                return;
            }
        }

        public bool StartActiveDay()
        {
            if (!hasBegun || currentPhase != DayLoopPhase.StartDay)
            {
                return false;
            }

            currentPhase = DayLoopPhase.ActiveDay;
            phaseElapsedSeconds = 0f;
            PhaseChanged?.Invoke(CreateSnapshot());
            return true;
        }

        public bool TryCompleteTask(string taskId)
        {
            if (!hasBegun || currentPhase != DayLoopPhase.ActiveDay)
            {
                return false;
            }

            string normalizedTaskId = NormalizeId(taskId);
            if (string.IsNullOrEmpty(normalizedTaskId) || !runtimeTaskLookup.TryGetValue(normalizedTaskId, out RuntimeTaskState task))
            {
                return false;
            }

            if (!task.TryComplete())
            {
                return false;
            }

            TaskChanged?.Invoke(task.ToSnapshot());
            TryUnlockDeferredTasks();

            if (AreAllTasksComplete())
            {
                EndLoop(DayLoopEndReason.SuccessfulLoop);
            }

            return true;
        }

        public bool TryGetTask(string taskId, out DayLoopTaskSnapshot taskSnapshot)
        {
            taskSnapshot = null;

            if (!hasBegun)
            {
                return false;
            }

            string normalizedTaskId = NormalizeId(taskId);
            if (string.IsNullOrEmpty(normalizedTaskId) || !runtimeTaskLookup.TryGetValue(normalizedTaskId, out RuntimeTaskState task))
            {
                return false;
            }

            taskSnapshot = task.ToSnapshot();
            return true;
        }

        public bool TryLearnKnowledge(string knowledgeId)
        {
            if (!hasBegun || currentPhase != DayLoopPhase.ActiveDay)
            {
                return false;
            }

            string normalizedKnowledgeId = NormalizeId(knowledgeId);
            if (string.IsNullOrEmpty(normalizedKnowledgeId) || !learnedKnowledge.Add(normalizedKnowledgeId))
            {
                return false;
            }

            KnowledgeLearned?.Invoke(normalizedKnowledgeId);
            return true;
        }

        public bool HasLearned(string knowledgeId)
        {
            string normalizedKnowledgeId = NormalizeId(knowledgeId);
            return !string.IsNullOrEmpty(normalizedKnowledgeId) && learnedKnowledge.Contains(normalizedKnowledgeId);
        }

        public void ForceReset()
        {
            if (!hasBegun)
            {
                return;
            }

            EndLoop(DayLoopEndReason.ManualReset);
        }

        private void StartNextLoop()
        {
            ActiveLoopIndex++;
            elapsedSeconds = 0f;
            phaseElapsedSeconds = 0f;
            currentPhase = DayLoopPhase.StartDay;

            foreach (RuntimeTaskState task in runtimeTasks)
            {
                task.ResetForNewLoop();
            }

            learnedKnowledge.Clear();
            hasBegun = true;
            TryUnlockDeferredTasks();
            LoopStarted?.Invoke(CreateSnapshot());
        }

        private void EndLoop(DayLoopEndReason reason)
        {
            LoopEnded?.Invoke(new DayLoopEndContext(reason, CreateSnapshot()));

            if (reason == DayLoopEndReason.SuccessfulLoop)
            {
                isFrozen = true;
                return;
            }

            StartNextLoop();
        }

        private bool AreAllTasksComplete()
        {
            foreach (RuntimeTaskState task in runtimeTasks)
            {
                if (!task.IsCompleted)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreCookingPrerequisitesComplete()
        {
            foreach (RuntimeTaskState task in runtimeTasks)
            {
                if (task.RequiredForCookingUnlock && !task.IsCompleted)
                {
                    return false;
                }
            }

            return true;
        }

        private void TryUnlockDeferredTasks()
        {
            if (!AreCookingPrerequisitesComplete())
            {
                return;
            }

            foreach (RuntimeTaskState task in runtimeTasks)
            {
                if (task.TryUnlock())
                {
                    TaskChanged?.Invoke(task.ToSnapshot());
                }
            }
        }

        private DayLoopSnapshot CreateSnapshot()
        {
            List<DayLoopTaskSnapshot> taskSnapshots = new List<DayLoopTaskSnapshot>(runtimeTasks.Count);
            foreach (RuntimeTaskState task in runtimeTasks)
            {
                taskSnapshots.Add(task.ToSnapshot());
            }

            List<string> knowledgeSnapshot = new List<string>(learnedKnowledge);
            knowledgeSnapshot.Sort(StringComparer.Ordinal);

            return new DayLoopSnapshot(
                ActiveLoopIndex,
                currentPhase,
                phaseElapsedSeconds,
                currentPhase == DayLoopPhase.StartDay ? startDayDurationSeconds : dayDurationSeconds,
                elapsedSeconds,
                dayDurationSeconds,
                taskSnapshots.AsReadOnly(),
                knowledgeSnapshot.AsReadOnly());
        }

        private static List<RuntimeTaskState> BuildRuntimeTasks(IEnumerable<DayLoopTaskDefinition> taskDefinitions)
        {
            if (taskDefinitions == null)
            {
                throw new ArgumentNullException(nameof(taskDefinitions));
            }

            List<RuntimeTaskState> tasks = new List<RuntimeTaskState>();
            HashSet<string> taskIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (DayLoopTaskDefinition definition in taskDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                string taskId = NormalizeId(definition.TaskId);
                if (string.IsNullOrEmpty(taskId))
                {
                    throw new ArgumentException("Every day-loop task requires a non-empty task id.", nameof(taskDefinitions));
                }

                if (!taskIds.Add(taskId))
                {
                    throw new ArgumentException(string.Format("Duplicate day-loop task id '{0}' found.", taskId), nameof(taskDefinitions));
                }

                if (!definition.StartsUnlocked && definition.RequiredForCookingUnlock)
                {
                    throw new ArgumentException(
                        string.Format("Task '{0}' cannot start locked and also be required for cooking unlock.", taskId),
                        nameof(taskDefinitions));
                }

                tasks.Add(new RuntimeTaskState(
                    taskId,
                    definition.DisplayName,
                    definition.StartsUnlocked,
                    definition.RequiredForCookingUnlock));
            }

            if (tasks.Count == 0)
            {
                throw new ArgumentException("At least one day-loop task is required.", nameof(taskDefinitions));
            }

            return tasks;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private sealed class RuntimeTaskState
        {
            public RuntimeTaskState(string taskId, string displayName, bool startsUnlocked, bool requiredForCookingUnlock)
            {
                TaskId = taskId;
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? taskId : displayName.Trim();
                StartsUnlocked = startsUnlocked;
                RequiredForCookingUnlock = requiredForCookingUnlock;
            }

            public string TaskId { get; }
            public string DisplayName { get; }
            public bool StartsUnlocked { get; }
            public bool RequiredForCookingUnlock { get; }
            public bool IsUnlocked { get; private set; }
            public bool IsCompleted { get; private set; }

            public void ResetForNewLoop()
            {
                IsUnlocked = StartsUnlocked;
                IsCompleted = false;
            }

            public bool TryUnlock()
            {
                if (IsUnlocked)
                {
                    return false;
                }

                IsUnlocked = true;
                return true;
            }

            public bool TryComplete()
            {
                if (!IsUnlocked || IsCompleted)
                {
                    return false;
                }

                IsCompleted = true;
                return true;
            }

            public DayLoopTaskSnapshot ToSnapshot()
            {
                return new DayLoopTaskSnapshot(TaskId, DisplayName, IsUnlocked, IsCompleted, RequiredForCookingUnlock);
            }
        }
    }
}
