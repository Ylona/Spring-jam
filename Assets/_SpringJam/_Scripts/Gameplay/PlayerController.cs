using SpringJam.Dialogue;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerController : MonoBehaviour, ILoopResetListener
{
    [SerializeField] private int speed = 3;

    private PlayerInputHandler input;
    private Rigidbody rb;
    private Vector3 movement;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        movement = CanMove() && input != null
            ? new Vector3(input.MoveInput.x, 0f, input.MoveInput.y).normalized
            : Vector3.zero;

        rb.MovePosition(transform.position + movement * speed * Time.fixedDeltaTime);
    }

    public void OnLoopReset()
    {
        movement = Vector3.zero;
    }

    private static bool CanMove()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        bool dayAllowsMovement = runtime == null || runtime.CurrentPhase == DayLoopPhase.ActiveDay;
        return dayAllowsMovement && !DialogueRuntimeController.IsDialogueOpen;
    }
}
