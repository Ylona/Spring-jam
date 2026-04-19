using System;
using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

namespace SpringJam.Dialogue
{
    public sealed class DialogueLine
    {
        public DialogueLine(string speakerName, string body)
        {
            SpeakerName = Normalize(speakerName);
            Body = Normalize(body);
        }

        public string SpeakerName { get; }
        public string Body { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class DialogueConversation
    {
        private readonly Action onCompleted;

        public DialogueConversation(string conversationId, string interactionText, IReadOnlyList<DialogueLine> lines, Action onCompleted = null)
        {
            ConversationId = Normalize(conversationId);
            InteractionText = string.IsNullOrWhiteSpace(interactionText) ? "Talk" : interactionText.Trim();
            Lines = lines ?? Array.Empty<DialogueLine>();
            this.onCompleted = onCompleted;
        }

        public string ConversationId { get; }
        public string InteractionText { get; }
        public IReadOnlyList<DialogueLine> Lines { get; }
        public int LineCount => Lines.Count;

        internal void Complete()
        {
            onCompleted?.Invoke();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class DialogueProgressSnapshot
    {
        public static DialogueProgressSnapshot Empty { get; } =
            new DialogueProgressSnapshot(Array.Empty<string>(), new Dictionary<string, bool>(StringComparer.Ordinal));

        private readonly HashSet<string> learnedKnowledge;
        private readonly Dictionary<string, bool> completedTasks;

        public DialogueProgressSnapshot(IEnumerable<string> learnedKnowledge, IEnumerable<KeyValuePair<string, bool>> completedTasks)
        {
            this.learnedKnowledge = new HashSet<string>(StringComparer.Ordinal);
            this.completedTasks = new Dictionary<string, bool>(StringComparer.Ordinal);

            if (learnedKnowledge != null)
            {
                foreach (string knowledgeId in learnedKnowledge)
                {
                    string normalizedKnowledgeId = Normalize(knowledgeId);
                    if (!string.IsNullOrEmpty(normalizedKnowledgeId))
                    {
                        this.learnedKnowledge.Add(normalizedKnowledgeId);
                    }
                }
            }

            if (completedTasks != null)
            {
                foreach (KeyValuePair<string, bool> taskState in completedTasks)
                {
                    string normalizedTaskId = Normalize(taskState.Key);
                    if (!string.IsNullOrEmpty(normalizedTaskId))
                    {
                        this.completedTasks[normalizedTaskId] = taskState.Value;
                    }
                }
            }
        }

        public bool HasLearned(string knowledgeId)
        {
            string normalizedKnowledgeId = Normalize(knowledgeId);
            return !string.IsNullOrEmpty(normalizedKnowledgeId) && learnedKnowledge.Contains(normalizedKnowledgeId);
        }

        public bool TryGetTaskCompleted(string taskId, out bool isCompleted)
        {
            string normalizedTaskId = Normalize(taskId);
            if (string.IsNullOrEmpty(normalizedTaskId))
            {
                isCompleted = false;
                return false;
            }

            return completedTasks.TryGetValue(normalizedTaskId, out isCompleted);
        }

        public static DialogueProgressSnapshot FromRuntime(DayLoopRuntime runtime)
        {
            if (runtime == null || runtime.CurrentSnapshot == null)
            {
                return Empty;
            }

            Dictionary<string, bool> taskStates = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (DayLoopTaskSnapshot task in runtime.CurrentSnapshot.Tasks)
            {
                string normalizedTaskId = Normalize(task.TaskId);
                if (!string.IsNullOrEmpty(normalizedTaskId))
                {
                    taskStates[normalizedTaskId] = task.IsCompleted;
                }
            }

            return new DialogueProgressSnapshot(runtime.CurrentSnapshot.LearnedKnowledge, taskStates);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class DialogueLineDefinition
    {
        [SerializeField] private string speakerName = string.Empty;
        [SerializeField]
        [TextArea(2, 4)]
        private string body = string.Empty;

        public bool HasContent => !string.IsNullOrWhiteSpace(body);

        public DialogueLine ToRuntimeLine(string fallbackSpeaker)
        {
            string resolvedSpeaker = string.IsNullOrWhiteSpace(speakerName) ? fallbackSpeaker : speakerName;
            return new DialogueLine(resolvedSpeaker, body);
        }
    }

    [Serializable]
    public sealed class DialogueConditionDefinition
    {
        [SerializeField] private List<string> requiredKnowledgeIds = new List<string>();
        [SerializeField] private List<string> blockedKnowledgeIds = new List<string>();
        [SerializeField] private List<string> requiredCompletedTaskIds = new List<string>();
        [SerializeField] private List<string> requiredIncompleteTaskIds = new List<string>();

        public DialogueConditionDefinition()
        {
        }

        public DialogueConditionDefinition(
            IEnumerable<string> requiredKnowledgeIds,
            IEnumerable<string> blockedKnowledgeIds,
            IEnumerable<string> requiredCompletedTaskIds,
            IEnumerable<string> requiredIncompleteTaskIds)
        {
            this.requiredKnowledgeIds = CreateList(requiredKnowledgeIds);
            this.blockedKnowledgeIds = CreateList(blockedKnowledgeIds);
            this.requiredCompletedTaskIds = CreateList(requiredCompletedTaskIds);
            this.requiredIncompleteTaskIds = CreateList(requiredIncompleteTaskIds);
        }

        public bool Matches(DialogueProgressSnapshot progress)
        {
            DialogueProgressSnapshot snapshot = progress ?? DialogueProgressSnapshot.Empty;
            return HasAllKnowledge(snapshot, requiredKnowledgeIds)
                && HasNoneOfKnowledge(snapshot, blockedKnowledgeIds)
                && MatchesTaskState(snapshot, requiredCompletedTaskIds, true)
                && MatchesTaskState(snapshot, requiredIncompleteTaskIds, false);
        }

        private static bool HasAllKnowledge(DialogueProgressSnapshot snapshot, IEnumerable<string> knowledgeIds)
        {
            foreach (string knowledgeId in EnumerateIds(knowledgeIds))
            {
                if (!snapshot.HasLearned(knowledgeId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasNoneOfKnowledge(DialogueProgressSnapshot snapshot, IEnumerable<string> knowledgeIds)
        {
            foreach (string knowledgeId in EnumerateIds(knowledgeIds))
            {
                if (snapshot.HasLearned(knowledgeId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesTaskState(DialogueProgressSnapshot snapshot, IEnumerable<string> taskIds, bool expectedCompletedState)
        {
            foreach (string taskId in EnumerateIds(taskIds))
            {
                if (!snapshot.TryGetTaskCompleted(taskId, out bool isCompleted) || isCompleted != expectedCompletedState)
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<string> EnumerateIds(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                yield break;
            }

            foreach (string id in ids)
            {
                string normalizedId = Normalize(id);
                if (!string.IsNullOrEmpty(normalizedId))
                {
                    yield return normalizedId;
                }
            }
        }

        private static List<string> CreateList(IEnumerable<string> ids)
        {
            List<string> values = new List<string>();
            foreach (string id in EnumerateIds(ids))
            {
                values.Add(id);
            }

            return values;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class DialogueSequenceDefinition
    {
        [SerializeField] private string sequenceId = string.Empty;
        [SerializeField] private string interactionText = "Talk";
        [SerializeField] private string speakerName = string.Empty;
        [SerializeField] private List<DialogueLineDefinition> lines = new List<DialogueLineDefinition>();
        [SerializeField] private List<string> knowledgeIdsToLearn = new List<string>();
        [SerializeField] private List<string> taskIdsToComplete = new List<string>();
        [SerializeField] private UnityEvent onSequenceCompleted;

        public string InteractionText => string.IsNullOrWhiteSpace(interactionText) ? "Talk" : interactionText.Trim();

        public bool HasContent
        {
            get
            {
                foreach (DialogueLineDefinition line in lines)
                {
                    if (line != null && line.HasContent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public DialogueConversation CreateConversation(Action onCompleted = null)
        {
            List<DialogueLine> runtimeLines = new List<DialogueLine>();
            string defaultSpeaker = string.IsNullOrWhiteSpace(speakerName) ? string.Empty : speakerName.Trim();

            foreach (DialogueLineDefinition line in lines)
            {
                if (line == null || !line.HasContent)
                {
                    continue;
                }

                runtimeLines.Add(line.ToRuntimeLine(defaultSpeaker));
            }

            return runtimeLines.Count == 0
                ? null
                : new DialogueConversation(sequenceId, InteractionText, runtimeLines.AsReadOnly(), onCompleted);
        }

        public void ApplyProgressionEffects(DayLoopRuntime runtime)
        {
            DayLoopProgressionApplier.Apply(runtime, knowledgeIdsToLearn, taskIdsToComplete);
            onSequenceCompleted?.Invoke();
        }
    }

    [Serializable]
    public sealed class NpcDialogueData
    {
        public List<ConditionalDialogueSequenceDefinition> variants = new List<ConditionalDialogueSequenceDefinition>();
    }

    [Serializable]
    public sealed class ConditionalDialogueSequenceDefinition
    {
        [SerializeField] private DialogueConditionDefinition conditions = new DialogueConditionDefinition();
        [SerializeField] private DialogueSequenceDefinition sequence = new DialogueSequenceDefinition();

        public DialogueSequenceDefinition Sequence => sequence;

        public bool Matches(DialogueProgressSnapshot progress)
        {
            return sequence != null
                && sequence.HasContent
                && (conditions == null || conditions.Matches(progress));
        }
    }
}



