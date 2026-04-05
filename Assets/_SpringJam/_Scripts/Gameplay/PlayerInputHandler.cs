using System;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public event Action OnInteract;

    private InputSystem_Actions controls;
    private bool isActive = true;


    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Enable();

        var runtime = DayLoopRuntime.Instance;
        if (runtime != null)
        {
            isActive = runtime.CurrentPhase == DayLoopPhase.ActiveDay;
            runtime.PhaseChanged += OnPhaseChanged;
        }
    }

    private void OnDisable()
    {
        controls.Disable();

        var runtime = DayLoopRuntime.Instance;
        if (runtime != null)
            runtime.PhaseChanged -= OnPhaseChanged;
    }
    private void OnPhaseChanged(DayLoopSnapshot snapshot)
    {
        isActive = snapshot.IsPlayablePhase;
        if (!isActive) MoveInput = Vector2.zero;
    }

    private void Update()
    {
        if (!isActive) return;

        MoveInput = controls.Player.Move.ReadValue<Vector2>();

        if (controls.Player.Interact.WasPressedThisFrame())
            OnInteract?.Invoke();
    }
}
