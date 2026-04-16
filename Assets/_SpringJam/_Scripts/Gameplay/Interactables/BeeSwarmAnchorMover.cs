using SpringJam.Systems.DayLoop;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BeeSwarmAnchorMover : MonoBehaviour
{
    [SerializeField] private Transform meadowAnchor;
    [SerializeField] private Transform greenhouseAnchor;

    private DayLoopRuntime subscribedRuntime;

    public Transform MeadowAnchor => meadowAnchor;
    public Transform GreenhouseAnchor => greenhouseAnchor;
    public bool IsAtGreenhouse { get; private set; }

    private void Awake()
    {
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

    [ContextMenu("Move To Greenhouse Anchor")]
    public void MoveToGreenhouseAnchor()
    {
        MoveToAnchor(greenhouseAnchor);
        IsAtGreenhouse = greenhouseAnchor != null;
    }

    [ContextMenu("Reset To Meadow Anchor")]
    public void ResetToMeadowAnchor()
    {
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