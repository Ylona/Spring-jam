using SpringJam.Systems.DayLoop;
using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class MaraHoneyDropper : MonoBehaviour
{
    [SerializeField] private string walkTaskId = "bloom-flowers";
    [SerializeField] private string dropTaskId = "guide-bees";
    [SerializeField] private Transform startAnchor;
    [SerializeField] private Transform dropAnchor;
    [SerializeField] private Transform honeyDropAnchor;
    [SerializeField] private Transform postDropAnchor;
    [SerializeField] private Transform honeyJar;
    [SerializeField] private Renderer[] honeyJarRenderers;
    [SerializeField] private Collider[] honeyJarColliders;
    [SerializeField] private SpriteRenderer maraSpriteRenderer;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float arrivalDistance = 0.05f;

    private DayLoopRuntime subscribedRuntime;
    private bool isWalkingToDrop;
    private bool isWaitingAtDrop;
    private bool isWalkingAfterDrop;
    private bool hasDroppedHoney;

    public bool IsWalkingToDrop => isWalkingToDrop;
    public bool IsWalkingAfterDrop => isWalkingAfterDrop;
    public bool HasDroppedHoney => hasDroppedHoney;

    private void Awake()
    {
        CacheReferences();
        ResetRoutine();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        CheckTasksOnStart();
    }

    private void Update()
    {
        UpdateDropMovement(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (!hasDroppedHoney)
        {
            SetHoneyAvailable(false);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // Called when bloom-flowers completes: Mara walks to the drop spot and waits.
    private void BeginWalk()
    {
        if (hasDroppedHoney || isWalkingToDrop || isWaitingAtDrop)
        {
            return;
        }

        isWalkingToDrop = true;
        SetMaraVisible(true);
    }

    // Called when guide-bees completes: Mara drops the honey (she may already be at the spot or still walking).
    private void AllowDrop()
    {
        if (hasDroppedHoney)
        {
            return;
        }

        if (isWaitingAtDrop)
        {
            DropHoney();
        }
        else
        {
            // She will drop as soon as she arrives (flag checked in UpdateDropMovement).
            isWaitingAtDrop = false; // repurpose: treat arrival as immediate drop
        }
    }

    private void UpdateDropMovement(float deltaTime)
    {
        if (isWalkingToDrop)
        {
            if (dropAnchor == null || MoveTowardAnchor(dropAnchor, deltaTime))
            {
                isWalkingToDrop = false;

                DayLoopRuntime runtime = DayLoopRuntime.Instance;
                bool dropTaskDone = runtime != null
                    && runtime.TryGetTask(NormalizeId(dropTaskId), out DayLoopTaskSnapshot snap)
                    && snap.IsCompleted;

                if (dropTaskDone)
                {
                    DropHoney();
                }
                else
                {
                    isWaitingAtDrop = true;
                }
            }

            return;
        }

        if (!isWalkingAfterDrop)
        {
            return;
        }

        if (postDropAnchor == null || MoveTowardAnchor(postDropAnchor, deltaTime))
        {
            isWalkingAfterDrop = false;
        }
    }

    private bool MoveTowardAnchor(Transform anchor, float deltaTime)
    {
        if (anchor == null)
        {
            return true;
        }

        Vector3 targetPosition = anchor.position;
        Vector3 offset = targetPosition - transform.position;
        if (offset.magnitude <= arrivalDistance)
        {
            transform.SetPositionAndRotation(targetPosition, anchor.rotation);
            return true;
        }

        Vector3 direction = offset.normalized;
        float step = Mathf.Max(0f, moveSpeed * deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
        if (maraSpriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
        {
            maraSpriteRenderer.flipX = direction.x < 0f;
        }

        if (Vector3.Distance(transform.position, targetPosition) > arrivalDistance)
        {
            return false;
        }

        transform.SetPositionAndRotation(targetPosition, anchor.rotation);
        return true;
    }

    private void DropHoney()
    {
        isWaitingAtDrop = false;
        hasDroppedHoney = true;

        Transform jarAnchor = honeyDropAnchor != null ? honeyDropAnchor : dropAnchor;
        if (jarAnchor != null && honeyJar != null)
        {
            honeyJar.SetPositionAndRotation(jarAnchor.position, jarAnchor.rotation);
        }

        SetHoneyAvailable(true);
        isWalkingAfterDrop = postDropAnchor != null;
    }

    private void ResetRoutine()
    {
        isWalkingToDrop = false;
        isWaitingAtDrop = false;
        isWalkingAfterDrop = false;
        hasDroppedHoney = false;

        if (startAnchor != null)
        {
            transform.SetPositionAndRotation(startAnchor.position, startAnchor.rotation);
        }

        SetMaraVisible(false);
        SetHoneyAvailable(false);
    }

    private void CheckTasksOnStart()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null) return;

        bool walkDone = runtime.TryGetTask(NormalizeId(walkTaskId), out DayLoopTaskSnapshot walkSnap) && walkSnap.IsCompleted;
        bool dropDone = runtime.TryGetTask(NormalizeId(dropTaskId), out DayLoopTaskSnapshot dropSnap) && dropSnap.IsCompleted;

        if (walkDone && dropDone)
        {
            // Both already done: skip straight to dropped state.
            isWalkingToDrop = false;
            isWaitingAtDrop = false;
            hasDroppedHoney = true;
            SetMaraVisible(true);
            SetHoneyAvailable(true);
        }
        else if (walkDone)
        {
            // Walk task done but not yet dropped: start walking.
            BeginWalk();
        }
    }

    private void HandleTaskChanged(DayLoopTaskSnapshot taskSnapshot)
    {
        if (taskSnapshot == null || !taskSnapshot.IsCompleted) return;

        string id = NormalizeId(taskSnapshot.TaskId);

        if (id == NormalizeId(walkTaskId))
        {
            BeginWalk();
        }
        else if (id == NormalizeId(dropTaskId))
        {
            AllowDrop();
        }
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        ResetRoutine();
    }

    private void SetMaraVisible(bool visible)
    {
        if (maraSpriteRenderer != null)
        {
            maraSpriteRenderer.enabled = visible;
        }
    }

    private void SetHoneyAvailable(bool available)
    {
        CacheReferences();

        if (honeyJarRenderers != null)
        {
            foreach (Renderer honeyRenderer in honeyJarRenderers)
            {
                if (honeyRenderer != null)
                {
                    honeyRenderer.enabled = available;
                }
            }
        }

        if (honeyJarColliders != null)
        {
            foreach (Collider honeyCollider in honeyJarColliders)
            {
                if (honeyCollider != null)
                {
                    honeyCollider.enabled = available;
                }
            }
        }
    }

    private void CacheReferences()
    {
        if (maraSpriteRenderer == null)
        {
            maraSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (honeyJar == null) return;

        if (honeyJarRenderers == null || honeyJarRenderers.Length == 0)
        {
            honeyJarRenderers = honeyJar.GetComponentsInChildren<Renderer>(true);
        }

        if (honeyJarColliders == null || honeyJarColliders.Length == 0)
        {
            honeyJarColliders = honeyJar.GetComponentsInChildren<Collider>(true);
        }
    }

    private void TrySubscribe()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null || subscribedRuntime == runtime) return;

        Unsubscribe();
        subscribedRuntime = runtime;
        subscribedRuntime.TaskChanged += HandleTaskChanged;
        subscribedRuntime.LoopStarted += HandleLoopStarted;
    }

    private void Unsubscribe()
    {
        if (subscribedRuntime == null) return;

        subscribedRuntime.TaskChanged -= HandleTaskChanged;
        subscribedRuntime.LoopStarted -= HandleLoopStarted;
        subscribedRuntime = null;
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
