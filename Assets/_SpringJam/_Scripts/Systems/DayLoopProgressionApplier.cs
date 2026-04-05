using System.Collections.Generic;

namespace SpringJam.Systems.DayLoop
{
    public static class DayLoopProgressionApplier
    {
        public static void Apply(DayLoopRuntime runtime, IEnumerable<string> knowledgeIds, IEnumerable<string> taskIds)
        {
            if (runtime == null)
            {
                return;
            }

            foreach (string knowledgeId in EnumerateIds(knowledgeIds))
            {
                runtime.TryLearnKnowledge(knowledgeId);
            }

            foreach (string taskId in EnumerateIds(taskIds))
            {
                runtime.TryCompleteTask(taskId);
            }
        }

        private static IEnumerable<string> EnumerateIds(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                yield break;
            }

            foreach (string id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    yield return id.Trim();
                }
            }
        }
    }
}
