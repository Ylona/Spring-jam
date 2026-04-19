using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using SpringJam2026.Audio;
using SpringJam2026.Utils;
using UnityEngine;
using UnityEngine.Events;

public class ItemInteractable : BaseInteractable
{
    [Header("Item")]
    [SerializeField] private bool canBePickedUp = true;
    [SerializeField] private string itemId = "item";
    [SerializeField] private string displayName = "Item";
    [SerializeField] private string pickupPrompt = "Pick Up";
    [SerializeField] private bool looseUseGravity = true;
    [SerializeField] private bool looseIsKinematic;

    [Header("Availability")]
    [SerializeField] private List<string> requiredCompletedTaskIds = new List<string>();
    [SerializeField] private string lockedPickupPrompt = "Unavailable";
    [SerializeField]
    [TextArea(2, 3)]
    private string lockedPickupMessage = "You cannot pick this up yet.";

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

        if (isPlaced && currentSocket != null && currentSocket.BlockPickupWhenPlaced)
        {
            return;
        }

        if (!canBePickedUp)
        {
            ApplyInteractionProgression();
            return;
        }

        if (!IsPickupUnlocked())
        {
            if (!string.IsNullOrWhiteSpace(lockedPickupMessage))
            {
                Debug.Log(lockedPickupMessage.Trim(), this);
            }

            return;
        }

        if (interactor.TryPickUpItem(this))
        {
            ApplyInteractionProgression();
            PlayPickupAudio(interactor.transform.position);
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
        if (IsHeld || (isPlaced && currentSocket != null && currentSocket.BlockPickupWhenPlaced))
        {
            return string.Empty;
        }

        if (!canBePickedUp)
        {
            return ResolvePickupPrompt(interactor);
        }

        if (!IsPickupUnlocked())
        {
            return ResolveLockedPrompt();
        }

        if (interactor != null && interactor.HasHeldItem)
        {
            return "Hands Full";
        }

        return ResolvePickupPrompt(interactor);
    }

    public void AttachToHolder(
        PlayerInteractor holder,
        Transform anchor,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        Transform target = ResolveAttachmentTarget(anchor, holder != null ? holder.HeldItemAnchor : null, "attach");
        if (target == null)
        {
            return;
        }

        RefreshSocketReferenceFromHierarchy();
        ReleaseFromSocket();

        currentHolder = holder;
        isPlaced = false;

        transform.SetParent(target, false);
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        transform.localScale = startingLocalScale;

        ApplyHeldState();
    }

    public bool PlaceIntoSocket(
        ItemSocketInteractable socket,
        Transform anchor,
        bool markAsLoopStartPlacement,
        bool invokePlacedEvent)
    {
        Transform target = ResolveAttachmentTarget(anchor, socket != null ? socket.SocketAnchor : null, "place");
        if (target == null)
        {
            return false;
        }

        ReleaseFromHolder();
        RefreshSocketReferenceFromHierarchy();

        if (currentSocket != null && currentSocket != socket)
        {
            ReleaseFromSocket();
        }

        currentSocket = socket;
        isPlaced = true;

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

        return true;
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

        if (loopStartSocket != null && PlaceIntoSocket(loopStartSocket, loopStartSocketAnchor, false, false))
        {
            loopStartSocket.RestoreLoopStartItemReference(this);
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
            ApplyKinematicState(false);
        }

        SetCollidersEnabled(false);
    }

    private void ApplySocketState()
    {
        if (attachedRigidbody != null)
        {
            ApplyKinematicState(false);
        }

        RestoreColliderStates();
    }

    private void ApplyLooseState()
    {
        if (attachedRigidbody != null)
        {
            if (looseIsKinematic)
            {
                ApplyKinematicState(looseUseGravity);
            }
            else
            {
                attachedRigidbody.isKinematic = false;
                attachedRigidbody.useGravity = looseUseGravity;
                attachedRigidbody.linearVelocity = Vector3.zero;
                attachedRigidbody.angularVelocity = Vector3.zero;
                attachedRigidbody.WakeUp();
            }
        }

        RestoreColliderStates();
    }

    private static void PlayPickupAudio(Vector3 position)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (ServiceLocator.TryGet<AudioService>(out AudioService audioService))
        {
            audioService.PlayPlayerPickupForage(position);
        }
    }

    private void ApplyKinematicState(bool useGravity)
    {
        if (attachedRigidbody == null)
        {
            return;
        }

        if (!attachedRigidbody.isKinematic)
        {
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
        }

        attachedRigidbody.useGravity = useGravity;
        attachedRigidbody.isKinematic = true;
        attachedRigidbody.Sleep();
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

    private Transform ResolveAttachmentTarget(Transform primaryTarget, Transform fallbackTarget, string action)
    {
        Transform target = primaryTarget != null ? primaryTarget : fallbackTarget;
        if (target == null)
        {
            Debug.LogWarning($"Cannot {action} {name} because no attachment target was provided.", this);
            return null;
        }

        if (target == transform)
        {
            Debug.LogWarning($"Cannot {action} {name} because it would parent itself.", this);
            return null;
        }

        return target;
    }

    private string ResolvePickupPrompt(PlayerInteractor interactor)
    {
        return string.IsNullOrWhiteSpace(pickupPrompt)
            ? base.GetInteractionText(interactor)
            : pickupPrompt.Trim();
    }

    private string ResolveLockedPrompt()
    {
        return string.IsNullOrWhiteSpace(lockedPickupPrompt)
            ? "Unavailable"
            : lockedPickupPrompt.Trim();
    }

    private bool IsPickupUnlocked()
    {
        if (requiredCompletedTaskIds == null || requiredCompletedTaskIds.Count == 0)
        {
            return true;
        }

        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null)
        {
            return false;
        }

        foreach (string taskId in requiredCompletedTaskIds)
        {
            string normalizedTaskId = NormalizeId(taskId);
            if (string.IsNullOrEmpty(normalizedTaskId))
            {
                continue;
            }

            if (!runtime.TryGetTask(normalizedTaskId, out DayLoopTaskSnapshot taskSnapshot) || !taskSnapshot.IsCompleted)
            {
                return false;
            }
        }

        return true;
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
