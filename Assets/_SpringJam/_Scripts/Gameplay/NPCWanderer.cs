using System.Collections;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class NPCWanderer : MonoBehaviour, ILoopResetListener
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float waitTimeMin = 1f;
    [SerializeField] private float waitTimeMax = 3f;
    [SerializeField] private float arrivalDistance = 0.1f;

    private Animator animator;
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");

    private int currentWaypointIndex = -1;
    private Coroutine wanderCoroutine;
    private Vector3 startPosition;
    private DayLoopRuntime subscribedRuntime;

    protected Transform[] Waypoints => waypoints;
    protected float MoveSpeedValue => moveSpeed;
    protected float ArrivalDistance => arrivalDistance;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        OnAwake();
    }

    private void Start()
    {
        TrySubscribe();

        if (waypoints == null || waypoints.Length == 0)
        {
            enabled = false;
            return;
        }

        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    private void OnDisable() => Unsubscribe();

    protected virtual void OnAwake() { }

    private IEnumerator WanderRoutine()
    {
        currentWaypointIndex = -1;

        while (true)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Transform waypoint = waypoints[currentWaypointIndex];

            yield return OnApproachingWaypoint(waypoint, currentWaypointIndex);
            yield return MoveToTarget(waypoint.position);

            SetAnimatorMoving(Vector2.zero);

            yield return OnArrivedAtWaypoint(waypoint, currentWaypointIndex);

            float waitTime = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(waitTime);
        }
    }

    protected virtual IEnumerator OnApproachingWaypoint(Transform waypoint, int index)
    {
        yield break;
    }

    protected virtual IEnumerator OnArrivedAtWaypoint(Transform waypoint, int index)
    {
        yield break;
    }

    protected IEnumerator MoveToTarget(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > arrivalDistance)
        {
            Vector3 dir = (target - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            SetAnimatorMoving(new Vector2(dir.x, dir.z));
            yield return null;
        }
    }

    protected void SetAnimatorMoving(Vector2 direction)
    {
        if (animator == null) return;

        bool moving = direction.sqrMagnitude > 0.01f;
        animator.SetBool(IsMoving, moving);

        if (moving)
        {
            animator.SetFloat(MoveX, direction.x);
            animator.SetFloat(MoveY, direction.y);
        }
    }

    private void HandleLoopStarted(DayLoopSnapshot _) => OnLoopReset();

    public virtual void OnLoopReset()
    {
        if (wanderCoroutine != null)
            StopCoroutine(wanderCoroutine);

        transform.position = startPosition;
        SetAnimatorMoving(Vector2.zero);
        currentWaypointIndex = -1;
        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    private void TrySubscribe()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null || subscribedRuntime == runtime) return;

        Unsubscribe();
        subscribedRuntime = runtime;
        subscribedRuntime.LoopStarted += HandleLoopStarted;
    }

    private void Unsubscribe()
    {
        if (subscribedRuntime == null) return;

        subscribedRuntime.LoopStarted -= HandleLoopStarted;
        subscribedRuntime = null;
    }
}
