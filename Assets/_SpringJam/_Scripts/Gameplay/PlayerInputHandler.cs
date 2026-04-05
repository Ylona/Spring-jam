using System;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public event Action OnInteract;

    private InputSystem_Actions controls;
    private DayLoopRuntime subscribedRuntime;
    private bool isActive = true;

    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Enable();
        SyncRuntimeSubscription();
    }

    private void OnDisable()
    {
        controls.Disable();
        UnsubscribeFromRuntime();
        MoveInput = Vector2.zero;
    }

    private void Update()
    {
        SyncRuntimeSubscription();

        if (!isActive)
        {
            MoveInput = Vector2.zero;
            return;
        }

        MoveInput = controls.Player.Move.ReadValue<Vector2>();

        if (controls.Player.Interact.WasPressedThisFrame())
        {
            OnInteract?.Invoke();
        }
    }

    private void SyncRuntimeSubscription()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == subscribedRuntime)
        {
            return;
        }

        UnsubscribeFromRuntime();
        subscribedRuntime = runtime;

        if (subscribedRuntime == null)
        {
            isActive = true;
            return;
        }

        isActive = subscribedRuntime.CurrentPhase == DayLoopPhase.ActiveDay;
        subscribedRuntime.LoopStarted += OnLoopStateChanged;
        subscribedRuntime.PhaseChanged += OnLoopStateChanged;
    }

    private void UnsubscribeFromRuntime()
    {
        if (subscribedRuntime == null)
        {
            return;
        }

        subscribedRuntime.LoopStarted -= OnLoopStateChanged;
        subscribedRuntime.PhaseChanged -= OnLoopStateChanged;
        subscribedRuntime = null;
    }

    private void OnLoopStateChanged(DayLoopSnapshot snapshot)
    {
        isActive = snapshot != null && snapshot.IsPlayablePhase;
        if (!isActive)
        {
            MoveInput = Vector2.zero;
        }
    }
}
