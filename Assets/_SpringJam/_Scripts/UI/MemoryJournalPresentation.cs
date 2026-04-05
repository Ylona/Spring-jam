using System;
using System.Collections.Generic;
using SpringJam.Systems.DayLoop;

namespace SpringJam.Journal
{
    public enum MemoryJournalTaskState
    {
        Sleeping,
        Ready,
        Complete,
    }

    public sealed class MemoryJournalTaskEntry
    {
        public MemoryJournalTaskEntry(string taskId, string title, string summary, string statusLabel, MemoryJournalTaskState state)
        {
            TaskId = Normalize(taskId);
            Title = Normalize(title);
            Summary = Normalize(summary);
            StatusLabel = Normalize(statusLabel);
            State = state;
        }

        public string TaskId { get; }
        public string Title { get; }
        public string Summary { get; }
        public string StatusLabel { get; }
        public MemoryJournalTaskState State { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MemoryJournalClueEntry
    {
        public MemoryJournalClueEntry(string knowledgeId, string title, string summary, string categoryLabel)
        {
            KnowledgeId = Normalize(knowledgeId);
            Title = Normalize(title);
            Summary = Normalize(summary);
            CategoryLabel = Normalize(categoryLabel);
        }

        public string KnowledgeId { get; }
        public string Title { get; }
        public string Summary { get; }
        public string CategoryLabel { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MemoryJournalPageData
    {
        public MemoryJournalPageData(
            string phaseLine,
            string tasksEmptyMessage,
            string cluesEmptyMessage,
            IReadOnlyList<MemoryJournalTaskEntry> tasks,
            IReadOnlyList<MemoryJournalClueEntry> clues)
        {
            PhaseLine = Normalize(phaseLine);
            TasksEmptyMessage = Normalize(tasksEmptyMessage);
            CluesEmptyMessage = Normalize(cluesEmptyMessage);
            Tasks = tasks ?? Array.Empty<MemoryJournalTaskEntry>();
            Clues = clues ?? Array.Empty<MemoryJournalClueEntry>();
        }

        public string PhaseLine { get; }
        public string TasksEmptyMessage { get; }
        public string CluesEmptyMessage { get; }
        public IReadOnlyList<MemoryJournalTaskEntry> Tasks { get; }
        public IReadOnlyList<MemoryJournalClueEntry> Clues { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class MemoryJournalPresentationBuilder
    {
        private static readonly IReadOnlyDictionary<string, TaskCopy> KnownTaskCopy =
            new Dictionary<string, TaskCopy>(StringComparer.Ordinal)
            {
                ["bloom-flowers"] = new TaskCopy(
                    "Bloom Order",
                    "The garden page stays still until the day is ready to be worked.",
                    "The flower beds still need the right order and a gentle hand.",
                    "The flower beds answered your care this loop."),
                ["guide-bees"] = new TaskCopy(
                    "Bee Routes",
                    "The bees are waiting on another part of the day to settle first.",
                    "The bees still need help finding the plants they belong with.",
                    "The bees found the plants they needed this loop."),
                ["learn-routines"] = new TaskCopy(
                    "Village Rhythms",
                    "These patterns have not opened themselves to you yet.",
                    "Someone in the valley is following a pattern worth remembering.",
                    "You traced the valley's routines clearly enough to act on them."),
                ["cook-spring-meal"] = new TaskCopy(
                    "Spring Meal",
                    "This page stays folded shut until the rest of the journal settles.",
                    "Everything else is in place. The meal can be prepared now.",
                    "The meal was finished before the light fell."),
            };

        private static readonly IReadOnlyDictionary<string, ClueCopy> KnownClueCopy =
            new Dictionary<string, ClueCopy>(StringComparer.Ordinal)
            {
                ["bunny-loop-hint"] = new ClueCopy(
                    "Bunny",
                    "Seeds Near the Old Path",
                    "The bunny tucked seeds near the old path before the light turns warm."),
            };

        public static MemoryJournalPageData Build(DayLoopSnapshot snapshot)
        {
            List<MemoryJournalTaskEntry> tasks = new List<MemoryJournalTaskEntry>();
            List<MemoryJournalClueEntry> clues = new List<MemoryJournalClueEntry>();

            if (snapshot != null)
            {
                foreach (DayLoopTaskSnapshot task in snapshot.Tasks)
                {
                    if (task != null)
                    {
                        tasks.Add(BuildTask(task));
                    }
                }

                foreach (string knowledgeId in snapshot.LearnedKnowledge)
                {
                    if (!string.IsNullOrWhiteSpace(knowledgeId))
                    {
                        clues.Add(BuildClue(knowledgeId));
                    }
                }
            }

            return new MemoryJournalPageData(
                BuildPhaseLine(snapshot),
                "The stitched task page is still blank.",
                "Empty pages wait for the valley to tell you something worth keeping.",
                tasks.AsReadOnly(),
                clues.AsReadOnly());
        }

        private static MemoryJournalTaskEntry BuildTask(DayLoopTaskSnapshot task)
        {
            TaskCopy copy = KnownTaskCopy.TryGetValue(task.TaskId, out TaskCopy knownCopy)
                ? knownCopy
                : TaskCopy.FromTask(task);

            MemoryJournalTaskState state = task.IsCompleted
                ? MemoryJournalTaskState.Complete
                : task.IsUnlocked
                    ? MemoryJournalTaskState.Ready
                    : MemoryJournalTaskState.Sleeping;

            string statusLabel = state switch
            {
                MemoryJournalTaskState.Sleeping => "Sleeping",
                MemoryJournalTaskState.Ready => "Ready",
                _ => "Complete",
            };

            string summary = state switch
            {
                MemoryJournalTaskState.Sleeping => copy.SleepingSummary,
                MemoryJournalTaskState.Ready => copy.ReadySummary,
                _ => copy.CompleteSummary,
            };

            return new MemoryJournalTaskEntry(
                task.TaskId,
                string.IsNullOrWhiteSpace(copy.Title) ? task.DisplayName : copy.Title,
                summary,
                statusLabel,
                state);
        }

        private static MemoryJournalClueEntry BuildClue(string knowledgeId)
        {
            if (KnownClueCopy.TryGetValue(knowledgeId, out ClueCopy copy))
            {
                return new MemoryJournalClueEntry(knowledgeId, copy.Title, copy.Summary, copy.CategoryLabel);
            }

            return new MemoryJournalClueEntry(
                knowledgeId,
                HumanizeId(knowledgeId),
                "You found a detail worth carrying into the next dawn.",
                "Memory");
        }

        private static string BuildPhaseLine(DayLoopSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "The pages are quiet until the valley wakes.";
            }

            if (snapshot.Phase == DayLoopPhase.StartDay)
            {
                return "Morning is settling in. The loop has not opened yet.";
            }

            if (TryGetTask(snapshot, "cook-spring-meal", out DayLoopTaskSnapshot cookingTask)
                && cookingTask.IsUnlocked
                && !cookingTask.IsCompleted)
            {
                return "The meal page has opened. The rest of the day's work is in place.";
            }

            if (AreAllTasksComplete(snapshot))
            {
                return "Everything the valley asked of you has been gathered.";
            }

            return snapshot.LearnedKnowledge.Count > 0
                ? "The day is moving. What you learn here will stay with you."
                : "The day is moving. The first real clue has not settled into the pages yet.";
        }

        private static bool AreAllTasksComplete(DayLoopSnapshot snapshot)
        {
            foreach (DayLoopTaskSnapshot task in snapshot.Tasks)
            {
                if (task != null && !task.IsCompleted)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetTask(DayLoopSnapshot snapshot, string taskId, out DayLoopTaskSnapshot taskSnapshot)
        {
            taskSnapshot = null;
            if (snapshot == null || string.IsNullOrWhiteSpace(taskId))
            {
                return false;
            }

            foreach (DayLoopTaskSnapshot task in snapshot.Tasks)
            {
                if (task != null && string.Equals(task.TaskId, taskId.Trim(), StringComparison.Ordinal))
                {
                    taskSnapshot = task;
                    return true;
                }
            }

            return false;
        }

        private static string HumanizeId(string value)
        {
            string normalizedValue = Normalize(value);
            if (normalizedValue.Length == 0)
            {
                return "Memory";
            }

            string[] words = normalizedValue
                .Replace("-", " ")
                .Replace("_", " ")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int index = 0; index < words.Length; index++)
            {
                string word = words[index];
                words[index] = word.Length == 1
                    ? char.ToUpperInvariant(word[0]).ToString()
                    : char.ToUpperInvariant(word[0]) + word.Substring(1);
            }

            return string.Join(" ", words);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private sealed class TaskCopy
        {
            public TaskCopy(string title, string sleepingSummary, string readySummary, string completeSummary)
            {
                Title = Normalize(title);
                SleepingSummary = Normalize(sleepingSummary);
                ReadySummary = Normalize(readySummary);
                CompleteSummary = Normalize(completeSummary);
            }

            public string Title { get; }
            public string SleepingSummary { get; }
            public string ReadySummary { get; }
            public string CompleteSummary { get; }

            public static TaskCopy FromTask(DayLoopTaskSnapshot task)
            {
                string title = Normalize(task?.DisplayName);
                if (title.Length == 0)
                {
                    title = HumanizeId(task?.TaskId);
                }

                return new TaskCopy(
                    title,
                    "This page is still waiting for the rest of the day to align.",
                    "This task is ready to be worked on this loop.",
                    "This task has been finished for the current loop.");
            }
        }

        private sealed class ClueCopy
        {
            public ClueCopy(string categoryLabel, string title, string summary)
            {
                CategoryLabel = Normalize(categoryLabel);
                Title = Normalize(title);
                Summary = Normalize(summary);
            }

            public string CategoryLabel { get; }
            public string Title { get; }
            public string Summary { get; }
        }
    }
}
