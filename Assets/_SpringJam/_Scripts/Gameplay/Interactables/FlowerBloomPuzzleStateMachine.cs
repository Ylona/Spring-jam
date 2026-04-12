using System;
using System.Collections.Generic;

public enum FlowerBedActivationResult
{
    Ignored,
    Progressed,
    Completed,
    Rejected,
}

public sealed class FlowerBloomPuzzleStateMachine
{
    private readonly List<string> orderedFlowerIds;

    public FlowerBloomPuzzleStateMachine(IEnumerable<string> orderedFlowerIds)
    {
        if (orderedFlowerIds == null)
        {
            throw new ArgumentNullException(nameof(orderedFlowerIds));
        }

        this.orderedFlowerIds = new List<string>();
        HashSet<string> knownFlowerIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string flowerId in orderedFlowerIds)
        {
            string normalizedFlowerId = NormalizeId(flowerId);
            if (string.IsNullOrEmpty(normalizedFlowerId))
            {
                throw new ArgumentException("Each flower bed requires a non-empty id.", nameof(orderedFlowerIds));
            }

            if (!knownFlowerIds.Add(normalizedFlowerId))
            {
                throw new ArgumentException(
                    $"Duplicate flower bed id '{normalizedFlowerId}' found in the bloom order.",
                    nameof(orderedFlowerIds));
            }

            this.orderedFlowerIds.Add(normalizedFlowerId);
        }

        if (this.orderedFlowerIds.Count == 0)
        {
            throw new ArgumentException("At least one flower bed is required for the bloom puzzle.", nameof(orderedFlowerIds));
        }
    }

    public int StepCount => orderedFlowerIds.Count;
    public int CurrentStepIndex { get; private set; }
    public bool IsCompleted { get; private set; }
    public string ExpectedFlowerId => IsCompleted ? string.Empty : orderedFlowerIds[CurrentStepIndex];

    public FlowerBedActivationResult TryActivate(string flowerId)
    {
        if (IsCompleted)
        {
            return FlowerBedActivationResult.Ignored;
        }

        string normalizedFlowerId = NormalizeId(flowerId);
        if (string.IsNullOrEmpty(normalizedFlowerId))
        {
            return FlowerBedActivationResult.Ignored;
        }

        if (!string.Equals(normalizedFlowerId, ExpectedFlowerId, StringComparison.Ordinal))
        {
            Reset();
            return FlowerBedActivationResult.Rejected;
        }

        CurrentStepIndex++;
        if (CurrentStepIndex < orderedFlowerIds.Count)
        {
            return FlowerBedActivationResult.Progressed;
        }
        IsCompleted = true;
        return FlowerBedActivationResult.Completed;
    }

    public void Reset()
    {
        CurrentStepIndex = 0;
        IsCompleted = false;
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}

