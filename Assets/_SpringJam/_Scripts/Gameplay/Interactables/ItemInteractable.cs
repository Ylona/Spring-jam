using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

public class ItemInteractable : BaseInteractable
{
    [Header("Item")]
    [SerializeField] private string itemId = "item";
    [SerializeField] private string displayName = "Item";
    [SerializeField] private string pickupPrompt = "Pick Up";
    [SerializeField] private bool looseUseGravity = true;
    [SerializeField] private bool looseIsKinematic;

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
    private PlayerInteractor currentHolder;
    private ItemSocketInteractable currentSocket;
    private ItemSocketInteractable loopStartSocket;
    private Transform loopStartSocketAnchor;
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
        RefreshSocketReferenceFromHierarchy();
        ReleaseFromSocket();

        currentHolder = holder;
        isPlaced = false;

        Transform target = anchor != null ? anchor : transform;
        transform.SetParent(target, false);
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        transform.localScale = startingLocalScale;

        ApplyHeldState();
    }

    public void PlaceIntoSocket(
        ItemSocketInteractable socket,
        Transform anchor,
        bool markAsLoopStartPlacement,
        bool invokePlacedEvent)
    {
        ReleaseFromHolder();
        RefreshSocketReferenceFromHierarchy();
        ReleaseFromSocket();

        currentSocket = socket;
        isPlaced = true;

        Transform target = anchor != null ? anchor : transform;
        transform.SetParent(target, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = startingLocalScale;

        ApplySocketState();

        if (markAsLoopStartPlacement)
        {
            loopStartSocket = socket;
            loopStartSocketAnchor = target;
        }

        if (invokePlacedEvent)
        {
            onPlaced?.Invoke();
        }
    }

    public void DropToWorld(Vector3 worldPosition)
    {
        ReleaseFromHolder();
        RefreshSocketReferenceFromHierarchy();
        ReleaseFromSocket();
        isPlaced = false;

        transform.SetParent(loopStartSocket != null ? null : startingParent, true);
        transform.position = worldPosition;
        transform.rotation = startingRotation;
        transform.localScale = startingLocalScale;

        ApplyLooseState();
        onDropped?.Invoke();
    }

    private void RestoreLoopStartState()
    {
        if (currentHolder != null)
        {
            PlayerInteractor holder = currentHolder;
            currentHolder = null;
            holder.ClearHeldItem(this);
        }

        currentSocket = null;
        isPlaced = false;

        if (loopStartSocket != null)
        {
            PlaceIntoSocket(loopStartSocket, loopStartSocketAnchor, false, false);
            return;
        }

        transform.SetParent(startingParent, true);
        transform.position = startingPosition;
        transform.rotation = startingRotation;
        transform.localScale = startingLocalScale;
        ApplyLooseState();
    }

    private void ApplyHeldState()
    {
        if (attachedRigidbody != null)
        {
            attachedRigidbody.useGravity = false;
            attachedRigidbody.isKinematic = true;
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
            attachedRigidbody.Sleep();
        }

        SetCollidersEnabled(false);
    }

    private void ApplySocketState()
    {
        if (attachedRigidbody != null)
        {
            attachedRigidbody.useGravity = false;
            attachedRigidbody.isKinematic = true;
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
            attachedRigidbody.Sleep();
        }

        RestoreColliderStates();
    }

    private void ApplyLooseState()
    {
        if (attachedRigidbody != null)
        {
            attachedRigidbody.useGravity = looseUseGravity;
            attachedRigidbody.isKinematic = looseIsKinematic;
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;

            if (looseIsKinematic)
            {
                attachedRigidbody.Sleep();
            }
            else
            {
                attachedRigidbody.WakeUp();
            }
        }

        RestoreColliderStates();
    }

    private void RestoreColliderStates()
    {
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

    private void ReleaseFromSocket()
    {
        if (currentSocket == null)
        {
            return;
        }

        ItemSocketInteractable socket = currentSocket;
        currentSocket = null;
        socket.ClearPlacedItem(this);
    }

    private void RefreshSocketReferenceFromHierarchy()
    {
        if (currentSocket != null)
        {
            return;
        }

        currentSocket = GetComponentInParent<ItemSocketInteractable>();
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
