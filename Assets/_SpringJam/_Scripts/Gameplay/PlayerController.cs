using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerController : MonoBehaviour, ILoopResetListener
{
    [SerializeField] private int speed = 3;

    private PlayerInputHandler input;
    private Rigidbody rb;
    private Animator animator;

    private Vector3 movement;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        if (input == null)
        {
            input = gameObject.AddComponent<PlayerInputHandler>();
        }

        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

    }


    private void Update()
    {
        if (animator == null) return;

        bool moving = movement.sqrMagnitude > 0f;
        animator.SetBool(IsMoving, moving);

        if (moving)
        {
            animator.SetFloat(MoveX, movement.x);
            animator.SetFloat(MoveY, movement.z);
        }
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
