using UnityEngine;

public class LeafDriftFall : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 1.2f;
    [SerializeField] private float driftStrength = 0.4f;
    [SerializeField] private float driftFrequency = 1.5f;
    [SerializeField] private Transform landingTarget;


    private Rigidbody rb;
    private bool isFalling;
    private float fallTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void StartFall()
    {
        gameObject.SetActive(true);
        isFalling = true;
        fallTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isFalling || rb == null || rb.isKinematic) return;

        fallTimer += Time.fixedDeltaTime;

        Vector3 current = rb.position;
        Vector3 target = new Vector3(landingTarget.position.x, current.y, landingTarget.position.z);
        Vector3 horizontal = (target - current);

        float drift = Mathf.Sin(fallTimer * driftFrequency) * driftStrength;
        Vector3 velocity = new Vector3(
            horizontal.x + drift,
            -fallSpeed,
            horizontal.z + drift * 0.5f
        );
        rb.linearVelocity = velocity;

        rb.angularVelocity = new Vector3(drift * 2f, 0f, 0f);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (!isFalling) return;
        if (!collision.gameObject.CompareTag("Ground")) return;

        isFalling = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        transform.rotation = Quaternion.identity;
    }

}
