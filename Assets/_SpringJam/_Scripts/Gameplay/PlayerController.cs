using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerController : MonoBehaviour, ILoopResetListener
{
    [SerializeField] private int speed = 3;

    private PlayerInputHandler input;

    private Rigidbody rb;
    private Vector3 movement;

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3(input.MoveInput.x, 0, input.MoveInput.y).normalized;
        rb.MovePosition(transform.position + movement * speed * Time.fixedDeltaTime);
    }

    public void OnLoopReset()
    {
        movement = Vector3.zero;
    }

}
