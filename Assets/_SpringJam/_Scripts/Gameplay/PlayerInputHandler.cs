using System;
using SpringJam.Dialogue;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public event Action OnInteract;

    private InputSystem_Actions controls;

    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
        MoveInput = Vector2.zero;
    }

    private void Update()
    {
        if (!CanProcessGameplayInput())
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

    private static bool CanProcessGameplayInput()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        bool dayAllowsInput = runtime == null || runtime.CurrentPhase == DayLoopPhase.ActiveDay;
        return dayAllowsInput && !DialogueRuntimeController.IsDialogueOpen;
    }
}
