using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerController : MonoBehaviour, ILoopResetListener
{
    [SerializeField] private int speed = 3;

    private InputSystem_Actions playerControlls;
    private Rigidbody rb;
    private Vector3 movement;

    void Awake()
    {
        playerControlls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        playerControlls.Enable();
    }

    private void OnDestroy()
    {
        playerControlls.Disable();
    }

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!CanMove())
        {
            movement = Vector3.zero;
            return;
        }

        float x = playerControlls.Player.Move.ReadValue<Vector2>().x;
        float z = playerControlls.Player.Move.ReadValue<Vector2>().y;

        movement = new Vector3(x, 0, z).normalized;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + movement * speed * Time.fixedDeltaTime);
    }

    public void OnLoopReset()
    {
        movement = Vector3.zero;
    }

    private static bool CanMove()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        return runtime == null || runtime.CurrentPhase == DayLoopPhase.ActiveDay;
    }
}
