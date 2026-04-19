using SpringJam.Systems.DayLoop;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BeeSwarmAnchorMover : MonoBehaviour
{
    [SerializeField] private Transform meadowAnchor;
    [SerializeField] private Transform greenhouseAnchor;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = Vector3.zero;
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float followSmoothTime = 0.3f;
    [SerializeField] private float trailingDistance = 0.35f;
    [SerializeField] private float hoverRadius = 0.08f;
    [SerializeField] private float hoverFrequency = 2.5f;
    [SerializeField] private float snapDistance = 0.02f;

    private Animator[] animators;
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    private DayLoopRuntime subscribedRuntime;
    private Transform activeFollowTarget;
    private Vector3 followVelocity;
    private Vector3 lastTargetPosition;
    private bool hasLastTargetPosition;

    public Transform MeadowAnchor => meadowAnchor;
    public Transform GreenhouseAnchor => greenhouseAnchor;
    public Transform FollowTarget => activeFollowTarget;
    public bool IsAtGreenhouse { get; private set; }

    private void Awake()
    {
        animators = GetComponentsInChildren<Animator>();
        ResetToMeadowAnchor();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        FollowActiveTarget(Time.deltaTime);
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        activeFollowTarget = target;
        ResetFollowVelocity();
    }

    [ContextMenu("Move To Greenhouse Anchor")]
    public void MoveToGreenhouseAnchor()
    {
        activeFollowTarget = followTarget != null ? followTarget : greenhouseAnchor;
        ResetFollowVelocity();
        IsAtGreenhouse = greenhouseAnchor != null;
    }

    [ContextMenu("Reset To Meadow Anchor")]
    public void ResetToMeadowAnchor()
    {
        activeFollowTarget = followTarget != null ? followTarget : meadowAnchor;
        ResetFollowVelocity();
        MoveToAnchor(meadowAnchor);
        IsAtGreenhouse = false;
    }

    private void MoveToAnchor(Transform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        transform.SetPositionAndRotation(anchor.position, anchor.rotation);
    }

    private void FollowActiveTarget(float deltaTime)
    {
        Transform target = activeFollowTarget != null ? activeFollowTarget : followTarget;
        if (target == null)
        {
            ResetFollowVelocity();
            return;
        }

        Vector3 targetPosition = ResolveNaturalFollowPosition(target);
        if (deltaTime <= 0f || followSpeed <= 0f || followSmoothTime <= 0f)
        {
            transform.position = targetPosition;
            return;
        }

        Vector3 toTarget = targetPosition - transform.position;
        Vector2 dir = new Vector2(toTarget.x, toTarget.z);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime,
            followSpeed,
            deltaTime);

        if (snapDistance > 0f && Vector3.Distance(transform.position, targetPosition) <= snapDistance)
        {
            transform.position = targetPosition;
            followVelocity = Vector3.zero;
        }

        SetAnimatorDirection(dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero);
    }

    private Vector3 ResolveNaturalFollowPosition(Transform target)
    {
        Vector3 targetPosition = target.position + followOffset;
        Vector3 targetDelta = hasLastTargetPosition ? target.position - lastTargetPosition : Vector3.zero;
        lastTargetPosition = target.position;
        hasLastTargetPosition = true;

        Vector3 horizontalDelta = new Vector3(targetDelta.x, 0f, targetDelta.z);
        if (trailingDistance > 0f && horizontalDelta.sqrMagnitude > 0.0001f)
        {
            targetPosition -= horizontalDelta.normalized * trailingDistance;
        }

        if (hoverRadius > 0f)
        {
            float hoverTime = Time.time * hoverFrequency;
            targetPosition += new Vector3(
                Mathf.Sin(hoverTime) * hoverRadius,
                Mathf.Sin(hoverTime * 1.31f) * hoverRadius * 0.35f,
                Mathf.Cos(hoverTime * 0.73f) * hoverRadius);
        }

        return targetPosition;
    }

    private void SetAnimatorDirection(Vector2 direction)
    {
        foreach (Animator animator in animators)
        {
            animator.SetFloat(MoveX, direction.x);
            animator.SetFloat(MoveY, direction.y);
        }
    }

    private void ResetFollowVelocity()
    {
        followVelocity = Vector3.zero;
        hasLastTargetPosition = false;
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        ResetToMeadowAnchor();
    }

    private void TrySubscribe()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null || subscribedRuntime == runtime)
        {
            return;
        }

        Unsubscribe();
        subscribedRuntime = runtime;
        subscribedRuntime.LoopStarted += HandleLoopStarted;
    }

    private void Unsubscribe()
    {
        if (subscribedRuntime == null)
        {
            return;
        }

        subscribedRuntime.LoopStarted -= HandleLoopStarted;
        subscribedRuntime = null;
    }
}