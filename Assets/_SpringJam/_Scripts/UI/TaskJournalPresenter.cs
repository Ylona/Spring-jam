using System;
using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using UnityEngine;

namespace SpringJam.UI
{
    public enum TaskJournalTaskState
    {
        Locked,
        Ready,
        Complete,
    }

    public enum TaskJournalTimeBand
    {
        Dawn,
        Morning,
        HighSun,
        LongLight,
        Dusk,
    }

    public readonly struct TaskJournalTaskPresentation
    {
        public TaskJournalTaskPresentation(
            string badgeText,
            string themeClass,
            string lockedText,
            string readyText,
            string completeText)
        {
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? "TASK" : badgeText.Trim();
            ThemeClass = string.IsNullOrWhiteSpace(themeClass) ? "task-card--default" : themeClass.Trim();
            LockedText = string.IsNullOrWhiteSpace(lockedText) ? "Waiting" : lockedText.Trim();
            ReadyText = string.IsNullOrWhiteSpace(readyText) ? "Ready" : readyText.Trim();
            CompleteText = string.IsNullOrWhiteSpace(completeText) ? "Complete" : completeText.Trim();
        }

        public string BadgeText { get; }
        public string ThemeClass { get; }
        public string LockedText { get; }
        public string ReadyText { get; }
        public string CompleteText { get; }

        public string GetStateText(TaskJournalTaskState state)
        {
            return state switch
            {
                TaskJournalTaskState.Locked => LockedText,
                TaskJournalTaskState.Ready => ReadyText,
                _ => CompleteText,
            };
        }
    }

    public static class TaskJournalPresenter
    {
        private static readonly TaskJournalTaskPresentation DefaultPresentation = new TaskJournalTaskPresentation(
            "TASK",
            "task-card--default",
            "Waiting",
            "Ready",
            "Complete");

        private static readonly Dictionary<string, TaskJournalTaskPresentation> TaskPresentations =
            new Dictionary<string, TaskJournalTaskPresentation>(StringComparer.Ordinal)
            {
                {
                    "bloom-flowers",
                    new TaskJournalTaskPresentation(
                        "BUD",
                        "task-card--flowers",
                        "Still sleeping",
                        "Ready to bloom",
                        "Blooming")
                },
                {
                    "guide-bees",
                    new TaskJournalTaskPresentation(
                        "HIVE",
                        "task-card--bees",
                        "Hive still quiet",
                        "Call the swarm",
                        "Pollen carried")
                },
                {
                    "learn-routines",
                    new TaskJournalTaskPresentation(
                        "PATH",
                        "task-card--routines",
                        "Listening for a clue",
                        "Watch the valley",
                        "Patterns learned")
                },
                {
                    "cook-spring-meal",
                    new TaskJournalTaskPresentation(
                        "FEAST",
                        "task-card--meal",
                        "Awaiting the valley",
                        "Ready for the table",
                        "Spring served")
                },
            };

        public static TaskJournalTaskPresentation GetTaskPresentation(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                return DefaultPresentation;
            }

            return TaskPresentations.TryGetValue(taskId.Trim(), out TaskJournalTaskPresentation presentation)
                ? presentation
                : DefaultPresentation;
        }

        public static TaskJournalTaskState GetTaskState(DayLoopTaskSnapshot task)
        {
            if (task == null || !task.IsUnlocked)
            {
                return TaskJournalTaskState.Locked;
            }

            return task.IsCompleted ? TaskJournalTaskState.Complete : TaskJournalTaskState.Ready;
        }

        public static string GetTaskStateText(DayLoopTaskSnapshot task)
        {
            TaskJournalTaskState state = GetTaskState(task);
            string taskId = task != null ? task.TaskId : string.Empty;
            return GetTaskPresentation(taskId).GetStateText(state);
        }

        public static TaskJournalTimeBand GetTimeBand(DayLoopSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return TaskJournalTimeBand.Dawn;
            }

            return GetTimeBand(snapshot.Phase, snapshot.ElapsedSeconds, snapshot.DayDurationSeconds);
        }

        public static TaskJournalTimeBand GetTimeBand(DayLoopPhase phase, float elapsedSeconds, float dayDurationSeconds)
        {
            float progress = GetNormalizedDayProgress(phase, elapsedSeconds, dayDurationSeconds);
            if (progress <= 0f)
            {
                return TaskJournalTimeBand.Dawn;
            }

            if (progress < 0.25f)
            {
                return TaskJournalTimeBand.Morning;
            }

            if (progress < 0.5f)
            {
                return TaskJournalTimeBand.HighSun;
            }

            if (progress < 0.75f)
            {
                return TaskJournalTimeBand.LongLight;
            }

            return TaskJournalTimeBand.Dusk;
        }

        public static string GetTimeLabel(DayLoopSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "Dawn hush";
            }

            return GetTimeLabel(snapshot.Phase, snapshot.ElapsedSeconds, snapshot.DayDurationSeconds);
        }

        public static string GetTimeLabel(DayLoopPhase phase, float elapsedSeconds, float dayDurationSeconds)
        {
            return GetTimeBand(phase, elapsedSeconds, dayDurationSeconds) switch
            {
                TaskJournalTimeBand.Dawn => "Dawn hush",
                TaskJournalTimeBand.Morning => "Morning bloom",
                TaskJournalTimeBand.HighSun => "High sun",
                TaskJournalTimeBand.LongLight => "Long light",
                _ => "Petals closing",
            };
        }

        public static int GetClosedPetalCount(DayLoopSnapshot snapshot, int petalCount)
        {
            if (snapshot == null)
            {
                return 0;
            }

            return GetClosedPetalCount(snapshot.Phase, snapshot.ElapsedSeconds, snapshot.DayDurationSeconds, petalCount);
        }

        public static int GetClosedPetalCount(DayLoopPhase phase, float elapsedSeconds, float dayDurationSeconds, int petalCount)
        {
            if (petalCount <= 0)
            {
                return 0;
            }

            float progress = GetNormalizedDayProgress(phase, elapsedSeconds, dayDurationSeconds);
            return Mathf.Clamp(Mathf.FloorToInt(progress * (petalCount + 1)), 0, petalCount);
        }

        public static float GetSunProgress(DayLoopSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return 0f;
            }

            return GetSunProgress(snapshot.Phase, snapshot.ElapsedSeconds, snapshot.DayDurationSeconds);
        }

        public static float GetSunProgress(DayLoopPhase phase, float elapsedSeconds, float dayDurationSeconds)
        {
            return GetNormalizedDayProgress(phase, elapsedSeconds, dayDurationSeconds);
        }

        private static float GetNormalizedDayProgress(DayLoopPhase phase, float elapsedSeconds, float dayDurationSeconds)
        {
            if (phase != DayLoopPhase.ActiveDay || dayDurationSeconds <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(elapsedSeconds / dayDurationSeconds);
        }
    }
}
