using SpringJam.Dialogue;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour, ILoopResetListener
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform interactPoint;

    [Header("Held Item")]
    [SerializeField] private Transform heldItemAnchor;
    [SerializeField] private Vector3 heldItemLocalPosition = new Vector3(0.45f, 0.5f, 0f);
    [SerializeField] private Vector3 heldItemLocalEulerAngles = Vector3.zero;
    [SerializeField] private float dropDistance = 0.75f;
    [SerializeField] private float dropHeightOffset = 0.15f;

    private static readonly Vector3 DefaultInteractionDirection = new Vector3(0f, 0f, 1f);

    private PlayerInputHandler input;
    private IInteractable currentInteractable;
    private ItemInteractable heldItem;
    private Vector3 lastInteractionDirection = DefaultInteractionDirection;

    public bool HasHeldItem => heldItem != null;
    public ItemInteractable HeldItem => heldItem;
    public Transform HeldItemAnchor => heldItemAnchor != null
        ? heldItemAnchor
        : interactPoint != null
            ? interactPoint
            : transform;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        if (input == null)
        {
            input = gameObject.AddComponent<PlayerInputHandler>();
        }

        input.OnInteract += TryInteract;
    }

    private void OnDisable()
    {
        DialogueRuntimeController.SetInteractionPrompt(string.Empty);
    }

    private void OnDestroy()
    {
        if (input != null)
        {
            input.OnInteract -= TryInteract;
        }
    }

    private void Update()
    {
        UpdateInteractionDirection();
        FindInteractable();
        UpdatePrompt();
    }

    private void TryInteract()
    {
        if (!IsInteractionEnabled() || DialogueRuntimeController.ConsumedInputThisFrame)
        {
            return;
        }

        if (currentInteractable != null)
        {
            currentInteractable.Interact(this);
            return;
        }

        DropHeldItem();
    }

    private void FindInteractable()
    {
        currentInteractable = null;
        if (interactPoint == null)
        {
            return;
        }

        int bestPriority = int.MinValue;
        float closestDistance = float.MaxValue;
        Collider[] hits = Physics.OverlapSphere(interactPoint.position, interactRange, interactableLayer);

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            int priority = GetInteractionPriority(interactable);
            float distance = Vector3.Distance(interactPoint.position, hit.transform.position);
            if (priority < bestPriority || (priority == bestPriority && distance >= closestDistance))
            {
                continue;
            }

            bestPriority = priority;
            closestDistance = distance;
            currentInteractable = interactable;
        }
    }

    private int GetInteractionPriority(IInteractable interactable)
    {
        if (interactable is ItemInteractable)
        {
            return 2;
        }

        if (HasHeldItem && interactable is ItemSocketInteractable)
        {
            return 1;
        }

        return 0;
    }

    private void UpdatePrompt()
    {
        string promptText = IsInteractionEnabled() && currentInteractable != null
            ? currentInteractable.GetInteractionText(this)
            : string.Empty;
        DialogueRuntimeController.SetInteractionPrompt(promptText);
    }

    private bool IsInteractionEnabled()
    {
        return input != null && input.IsGameplayInputEnabled;
    }

    private void UpdateInteractionDirection()
    {
        if (input == null)
        {
            return;
        }

        Vector2 moveInput = input.MoveInput;
        if (moveInput.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        lastInteractionDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
    }

    public bool TryPickUpItem(ItemInteractable item)
    {
        if (item == null || heldItem != null)
        {
            return false;
        }

        heldItem = item;
        heldItem.AttachToHolder(
            this,
            HeldItemAnchor,
            heldItemLocalPosition,
            Quaternion.Euler(heldItemLocalEulerAngles));
        return true;
    }

    public bool TryPlaceHeldItem(ItemSocketInteractable socket)
    {
        if (socket == null || heldItem == null || !socket.CanPlace(heldItem))
        {
            return false;
        }

        ItemInteractable itemToPlace = heldItem;
        heldItem = null;
        socket.PlaceItem(itemToPlace);
        return true;
    }

    public bool DropHeldItem()
    {
        if (heldItem == null)
        {
            return false;
        }

        ItemInteractable itemToDrop = heldItem;
        heldItem = null;

        Vector3 basePosition = interactPoint != null ? interactPoint.position : transform.position;
        Vector3 dropPosition = basePosition + lastInteractionDirection * dropDistance + Vector3.up * dropHeightOffset;
        itemToDrop.DropToWorld(dropPosition);
        return true;
    }

    public void ClearHeldItem(ItemInteractable item)
    {
        if (heldItem == item)
        {
            heldItem = null;
        }
    }

    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }

    public void OnLoopReset()
    {
        heldItem = null;
        currentInteractable = null;
        lastInteractionDirection = DefaultInteractionDirection;
        DialogueRuntimeController.SetInteractionPrompt(string.Empty);
    }

    private void OnDrawGizmosSelected()
    {
        if (interactPoint == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactPoint.position, interactRange);
    }
}
