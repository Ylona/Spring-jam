using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using SpringJam2026.Audio;
using SpringJam2026.Utils;
using UnityEngine;
using UnityEngine.Events;

public class ItemSocketInteractable : BaseInteractable
{
    [Header("Socket")]
    [SerializeField] private Transform socketAnchor;
    [SerializeField] private List<string> acceptedItemIds = new List<string>();
    [SerializeField] private string placementPrompt = "Place Item";
    [SerializeField] private string taskIdOnPlacement = string.Empty;
    [SerializeField] private ItemInteractable startingItem;

    [Header("Availability")]
    [SerializeField] private List<string> requiredCompletedTaskIds = new List<string>();
    [SerializeField] private string lockedPlacementPrompt = "Unavailable";
    [SerializeField]
    [TextArea(2, 3)]
    private string lockedPlacementMessage = "You cannot place this here yet.";

    [Header("Feedback")]
    [SerializeField] private bool playBeeMovementFeedbackOnPlacement;

    [Header("Bee Swarm")]
    [SerializeField] private bool moveBeeSwarmOnPlacement;
    [SerializeField] private BeeSwarmAnchorMover beeSwarmMoverOnPlacement;

    [Header("Events")]
    [SerializeField] private UnityEvent onItemPlaced;

    private DayLoopRuntime subscribedRuntime;
    private ItemInteractable placedItem;
    private ItemInteractable loopStartItem;

    public bool HasPlacedItem => placedItem != null;
    public Transform SocketAnchor => socketAnchor != null ? socketAnchor : transform;

    private void Awake()
    {
        loopStartItem = startingItem;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        ApplyStartingItemPlacement();
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

        if (placedItem != null)
        {
            Debug.Log("There is already something placed here.", this);
            return;
        }

        if (!interactor.HasHeldItem)
        {
            Debug.Log("You need to be carrying something to place it here.", this);
            return;
        }

        if (!CanAccept(interactor.HeldItem))
        {
            Debug.Log($"This spot does not accept {interactor.HeldItem.DisplayName}.", this);
            return;
        }

        if (!IsPlacementUnlocked())
        {
            if (!string.IsNullOrWhiteSpace(lockedPlacementMessage))
            {
                Debug.Log(lockedPlacementMessage.Trim(), this);
            }

            return;
        }

        if (interactor.TryPlaceHeldItem(this))
        {
            DayLoopRuntime.Instance?.TryCompleteTask(taskIdOnPlacement);
        }
    }

    public override string GetInteractionText(PlayerInteractor interactor)
    {
        if (placedItem != null)
        {
            return "Occupied";
        }

        if (interactor == null || !interactor.HasHeldItem)
        {
            return placementPrompt;
        }

        if (!CanAccept(interactor.HeldItem))
        {
            return "Wrong Item";
        }

        return IsPlacementUnlocked() ? placementPrompt : ResolveLockedPrompt();
    }

    public bool CanAccept(ItemInteractable item)
    {
        if (item == null)
        {
            return false;
        }

        if (acceptedItemIds == null || acceptedItemIds.Count == 0)
        {
            return true;
        }

        string heldItemId = item.ItemId;
        foreach (string acceptedItemId in acceptedItemIds)
        {
            if (NormalizeId(acceptedItemId) == heldItemId)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanPlace(ItemInteractable item)
    {
        return CanAccept(item) && IsPlacementUnlocked();
    }

    public void PlaceItem(ItemInteractable item)
    {
        if (item == null || !CanPlace(item))
        {
            return;
        }

        placedItem = item;
        placedItem.PlaceIntoSocket(this, SocketAnchor, false, true);
        onItemPlaced?.Invoke();
        MoveBeeSwarm();
        PlayPlacementFeedback();
    }

    public void ClearPlacedItem(ItemInteractable item)
    {
        if (placedItem != item)
        {
            return;
        }

        placedItem = null;

        // Keep the serialized field aligned with live scene occupancy during play.
        if (startingItem == item)
        {
            startingItem = null;
        }
    }

    private bool IsPlacementUnlocked()
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

    private string ResolveLockedPrompt()
    {
        return string.IsNullOrWhiteSpace(lockedPlacementPrompt)
            ? "Unavailable"
            : lockedPlacementPrompt.Trim();
    }

    private void PlayPlacementFeedback()
    {
        if (!playBeeMovementFeedbackOnPlacement || !Application.isPlaying)
        {
            return;
        }

        try
        {
            ServiceLocator.Get<AudioService>()?.PlayBeeMovement(SocketAnchor.position);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Could not play bee movement feedback: {exception.Message}", this);
        }
    }

    private void MoveBeeSwarm()
    {
        if (!moveBeeSwarmOnPlacement)
        {
            return;
        }

        BeeSwarmAnchorMover mover = beeSwarmMoverOnPlacement;
        if (mover == null && Application.isPlaying)
        {
            mover = FindFirstObjectByType<BeeSwarmAnchorMover>();
        }

        if (mover == null)
        {
            Debug.LogWarning("No bee swarm mover is assigned for this placement socket.", this);
            return;
        }

        mover.MoveToGreenhouseAnchor();
    }

    private void ApplyStartingItemPlacement()
    {
        if (loopStartItem == null)
        {
            startingItem = null;
            return;
        }

        placedItem = loopStartItem;
        startingItem = loopStartItem;
        loopStartItem.PlaceIntoSocket(this, SocketAnchor, true, false);
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        placedItem = null;
        startingItem = loopStartItem;
        ApplyStartingItemPlacement();
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