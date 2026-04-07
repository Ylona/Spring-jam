using System;
using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class FlowerBloomPuzzleController : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private string taskId = "bloom-flowers";
    [SerializeField] private List<FlowerBedInteractable> orderedFlowerBeds = new List<FlowerBedInteractable>();

    [Header("Events")]
    [SerializeField] private UnityEvent onPuzzleProgressed;
    [SerializeField] private UnityEvent onPuzzleReset;
    [SerializeField] private UnityEvent onPuzzleCompleted;

    private readonly Dictionary<FlowerBedInteractable, int> bedOrderLookup = new Dictionary<FlowerBedInteractable, int>();

    private DayLoopRuntime subscribedRuntime;
    private FlowerBloomPuzzleStateMachine stateMachine;
    private bool hasInitialized;

    public bool IsCompleted => stateMachine != null && stateMachine.IsCompleted;
    public int CurrentStepIndex => stateMachine != null ? stateMachine.CurrentStepIndex : 0;
    public string ExpectedFlowerId => stateMachine != null ? stateMachine.ExpectedFlowerId : string.Empty;
    public int StepCount => stateMachine != null ? stateMachine.StepCount : 0;

    private void Awake()
    {
        TryAssignControllerToBeds();
        ApplyResetState(false, false);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        TryAssignControllerToBeds();
        hasInitialized = false;
        stateMachine = null;
        bedOrderLookup.Clear();
    }

    [ContextMenu("Reset Puzzle State")]
    public void ResetPuzzleState()
    {
        ApplyResetState(true, true);
    }

    public FlowerBedActivationResult TryActivateBed(FlowerBedInteractable flowerBed)
    {
        if (flowerBed == null || !EnsureInitialized())
        {
            return FlowerBedActivationResult.Ignored;
        }

        if (!bedOrderLookup.ContainsKey(flowerBed))
        {
            Debug.LogWarning($"Flower bed '{flowerBed.name}' is not registered in the bloom puzzle order.", flowerBed);
            return FlowerBedActivationResult.Ignored;
        }

        FlowerBedActivationResult result = stateMachine.TryActivate(flowerBed.FlowerId);
        switch (result)
        {
            case FlowerBedActivationResult.Progressed:
                flowerBed.NotifyActivated();
                onPuzzleProgressed?.Invoke();
                break;
            case FlowerBedActivationResult.Completed:
                flowerBed.NotifyActivated();
                DayLoopRuntime.Instance?.TryCompleteTask(taskId);
                onPuzzleCompleted?.Invoke();
                break;
            case FlowerBedActivationResult.Rejected:
                flowerBed.NotifyRejectedInteraction();
                ApplyResetState(true, true);
                break;
        }

        return result;
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        ApplyResetState(true, true);
    }

    private void ApplyResetState(bool invokePuzzleResetEvent, bool invokeBedResetEvents)
    {
        if (!EnsureInitialized())
        {
            return;
        }

        stateMachine.Reset();
        foreach (FlowerBedInteractable flowerBed in orderedFlowerBeds)
        {
            flowerBed?.ResetState(invokeBedResetEvents);
        }

        if (invokePuzzleResetEvent)
        {
            onPuzzleReset?.Invoke();
        }
    }

    private bool EnsureInitialized()
    {
        if (hasInitialized)
        {
            return stateMachine != null;
        }

        hasInitialized = true;
        stateMachine = null;
        bedOrderLookup.Clear();

        if (orderedFlowerBeds == null || orderedFlowerBeds.Count == 0)
        {
            Debug.LogError("Flower bloom puzzle requires at least one configured flower bed.", this);
            return false;
        }

        List<string> orderedFlowerIds = new List<string>(orderedFlowerBeds.Count);
        for (int i = 0; i < orderedFlowerBeds.Count; i++)
        {
            FlowerBedInteractable flowerBed = orderedFlowerBeds[i];
            if (flowerBed == null)
            {
                Debug.LogError($"Flower bloom puzzle has a missing bed reference at order index {i}.", this);
                return false;
            }

            if (bedOrderLookup.ContainsKey(flowerBed))
            {
                Debug.LogError($"Flower bed '{flowerBed.name}' is listed more than once in the bloom order.", this);
                return false;
            }

            flowerBed.SetPuzzleController(this);
            bedOrderLookup.Add(flowerBed, i);
            orderedFlowerIds.Add(flowerBed.FlowerId);
        }

        try
        {
            stateMachine = new FlowerBloomPuzzleStateMachine(orderedFlowerIds);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            stateMachine = null;
            return false;
        }
    }

    private void TryAssignControllerToBeds()
    {
        if (orderedFlowerBeds == null)
        {
            return;
        }

        foreach (FlowerBedInteractable flowerBed in orderedFlowerBeds)
        {
            flowerBed?.SetPuzzleController(this);
        }
    }

    private void TrySubscribe()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null || subscribedRuntime == runtime)
        {
            return;
        }

        Unsubscribe();
        subscribedRuntime = runtime;
        subscribedRuntime.LoopStarted += HandleLoopStarted;
    }

    private void Unsubscribe()
    {
        if (subscribedRuntime == null)
        {
            return;
        }

        subscribedRuntime.LoopStarted -= HandleLoopStarted;
        subscribedRuntime = null;
    }
}
