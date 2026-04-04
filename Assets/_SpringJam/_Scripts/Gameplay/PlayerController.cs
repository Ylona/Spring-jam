using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private int speed;

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

    // Update is called once per frame
    void Update()
    {
        float x = playerControlls.Player.Move.ReadValue<Vector2>().x;
        float z = playerControlls.Player.Move.ReadValue<Vector2>().y;

        movement = new Vector3(x, 0, z).normalized;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + movement * speed * Time.fixedDeltaTime);
    }
}
