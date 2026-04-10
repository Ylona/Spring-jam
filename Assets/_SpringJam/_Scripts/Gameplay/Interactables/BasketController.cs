using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class BasketController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private string taskId = "learn-routines";
    [SerializeField] private BasketInteractable basket;

    [Header("Timing")]
    [Range(0,1.0f)]
    [SerializeField] private float hideAtNormalizedTime = 0.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onBasketShown;
    [SerializeField] private UnityEvent onBasketHidden;
    [SerializeField] private UnityEvent onBasketCollected;

    private DayLoopRuntime subscribedRuntime;
    private bool isHidden;
    private bool hasCollected;

    private void Awake()
    {
        TryAssignBasket();
        ResetState(false);
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
        if (isHidden || hasCollected || basket == null || basket.IsHeld)
        {
            return;
        }

        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null)
        {
            return;
        }

        float progress = runtime.CurrentSnapshot?.NormalizedProgress ?? 0f;

        if (progress >= hideAtNormalizedTime)
        {
            HideBasket();
        }
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        ResetState(true);
    }

    private void ResetState(bool invokeEvents)
    {
        isHidden = false;
        hasCollected = false;

        if (basket != null)
        {
            basket.gameObject.SetActive(true);
        }

        if (invokeEvents)
        {
            onBasketShown?.Invoke();
        }
    }

    private void HideBasket()
    {
        if (basket == null)
        {
            return;
        }

        isHidden = true;
        basket.gameObject.SetActive(false);
        onBasketHidden?.Invoke();
    }

    public void NotifyBasketCollected()
    {
        if (hasCollected)
        {
            return;
        }

        hasCollected = true;

        DayLoopRuntime.Instance?.TryCompleteTask(taskId);
        onBasketCollected?.Invoke();
    }

    private void TryAssignBasket()
    {
        if (basket == null)
        {
            basket = GetComponentInChildren<BasketInteractable>();
        }

        if (basket != null)
        {
            basket.SetController(this);
        }
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