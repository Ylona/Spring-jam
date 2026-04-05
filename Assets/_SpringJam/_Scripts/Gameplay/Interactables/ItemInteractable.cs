using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

public class ItemInteractable : BaseInteractable
{
    [Header("Item")]
    [SerializeField] private string itemId = "item";
    [SerializeField] private string displayName = "Item";
    [SerializeField] private string pickupPrompt = "Pick Up";

    [Header("Events")]
    [SerializeField] private UnityEvent onPickedUp;
    [SerializeField] private UnityEvent onDropped;
    [SerializeField] private UnityEvent onPlaced;

    private DayLoopRuntime subscribedRuntime;
    private Transform startingParent;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Vector3 startingLocalScale;
    private Rigidbody attachedRigidbody;
    private Collider[] colliders;
    private bool[] colliderEnabledStates;
    private bool cachedUseGravity;
    private bool cachedIsKinematic;
    private PlayerInteractor currentHolder;
    private bool isPlaced;

    public string ItemId => NormalizeId(itemId);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim();
    public bool IsHeld => currentHolder != null;
    public bool IsPlaced => isPlaced;

    private void Awake()
    {
        startingParent = transform.parent;
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        startingLocalScale = transform.localScale;

        attachedRigidbody = GetComponent<Rigidbody>();
        if (attachedRigidbody != null)
        {
            cachedUseGravity = attachedRigidbody.useGravity;
            cachedIsKinematic = attachedRigidbody.isKinematic;
        }

        colliders = GetComponentsInChildren<Collider>(true);
        colliderEnabledStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderEnabledStates[i] = colliders[i].enabled;
        }
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

    public override void Interact(PlayerInteractor interactor)
    {
        if (interactor == null)
        {
            return;
        }

        if (interactor.TryPickUpItem(this))
        {
            onPickedUp?.Invoke();
            return;
        }

        if (interactor.HasHeldItem)
        {
            Debug.Log($"Your hands are already full. You are carrying {interactor.HeldItem.DisplayName}.", this);
        }
    }

    public override string GetInteractionText(PlayerInteractor interactor)
    {
        if (IsHeld)
        {
            return string.Empty;
        }

        if (interactor != null && interactor.HasHeldItem)
        {
            return "Hands Full";
        }

        return string.IsNullOrWhiteSpace(pickupPrompt)
            ? base.GetInteractionText(interactor)
            : pickupPrompt.Trim();
    }

    public void AttachToHolder(
        PlayerInteractor holder,
        Transform anchor,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        currentHolder = holder;
        isPlaced = false;
        AttachToTarget(anchor, localPosition, localRotation);
    }

    public void PlaceIntoSocket(Transform anchor)
    {
        ReleaseFromHolder();
        isPlaced = true;
        AttachToTarget(anchor, Vector3.zero, Quaternion.identity);
        onPlaced?.Invoke();
    }

    public void DropToWorld(Vector3 worldPosition)
    {
        ReleaseFromHolder();
        isPlaced = false;

        transform.SetParent(startingParent, true);
        transform.position = worldPosition;
        transform.rotation = startingRotation;
        transform.localScale = startingLocalScale;

        RestorePhysicsState();
        onDropped?.Invoke();
    }

    private void RestoreLoopStartState()
    {
        ReleaseFromHolder();
        isPlaced = false;

        transform.SetParent(startingParent, true);
        transform.position = startingPosition;
        transform.rotation = startingRotation;
        transform.localScale = startingLocalScale;

        RestorePhysicsState();

        if (attachedRigidbody != null)
        {
            attachedRigidbody.position = startingPosition;
            attachedRigidbody.rotation = startingRotation;
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
            attachedRigidbody.Sleep();
        }
    }

    private void AttachToTarget(Transform anchor, Vector3 localPosition, Quaternion localRotation)
    {
        transform.SetParent(anchor, false);
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;

        if (attachedRigidbody != null)
        {
            attachedRigidbody.useGravity = false;
            attachedRigidbody.isKinematic = true;
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
        }

        SetCollidersEnabled(false);
    }

    private void RestorePhysicsState()
    {
        if (attachedRigidbody != null)
        {
            attachedRigidbody.useGravity = cachedUseGravity;
            attachedRigidbody.isKinematic = cachedIsKinematic;
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = colliderEnabledStates[i];
            }
        }
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        foreach (Collider itemCollider in colliders)
        {
            if (itemCollider != null)
            {
                itemCollider.enabled = isEnabled;
            }
        }
    }

    private void ReleaseFromHolder()
    {
        if (currentHolder == null)
        {
            return;
        }

        PlayerInteractor holder = currentHolder;
        currentHolder = null;
        holder.ClearHeldItem(this);
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        RestoreLoopStartState();
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

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
