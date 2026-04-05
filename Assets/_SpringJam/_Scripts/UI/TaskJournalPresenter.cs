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

    public static class TaskJournalPresenter
    {
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

            switch (taskId)
            {
                case "bloom-flowers":
                    return state switch
                    {
                        TaskJournalTaskState.Locked => "Still sleeping",
                        TaskJournalTaskState.Ready => "Ready to bloom",
                        _ => "Blooming",
                    };
                case "guide-bees":
                    return state switch
                    {
                        TaskJournalTaskState.Locked => "Hive still quiet",
                        TaskJournalTaskState.Ready => "Call the swarm",
                        _ => "Pollen carried",
                    };
                case "learn-routines":
                    return state switch
                    {
                        TaskJournalTaskState.Locked => "Listening for a clue",
                        TaskJournalTaskState.Ready => "Watch the valley",
                        _ => "Patterns learned",
                    };
                case "cook-spring-meal":
                    return state switch
                    {
                        TaskJournalTaskState.Locked => "Awaiting the valley",
                        TaskJournalTaskState.Ready => "Ready for the table",
                        _ => "Spring served",
                    };
                default:
                    return state switch
                    {
                        TaskJournalTaskState.Locked => "Waiting",
                        TaskJournalTaskState.Ready => "Ready",
                        _ => "Complete",
                    };
            }
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
