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
        movement = input != null
            ? new Vector3(input.MoveInput.x, 0f, input.MoveInput.y).normalized
            : Vector3.zero;

        if (movement.sqrMagnitude > 0f && rb.IsSleeping())
        {
            rb.WakeUp();
        }

        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    public void OnLoopReset()
    {
        movement = Vector3.zero;
    }
}
