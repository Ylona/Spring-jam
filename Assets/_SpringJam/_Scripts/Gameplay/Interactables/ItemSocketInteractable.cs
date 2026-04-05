using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

public class ItemSocketInteractable : BaseInteractable
{
    [Header("Socket")]
    [SerializeField] private Transform socketAnchor;
    [SerializeField] private List<string> acceptedItemIds = new List<string>();
    [SerializeField] private string placementPrompt = "Place Item";
    [SerializeField] private string taskIdOnPlacement = string.Empty;

    [Header("Events")]
    [SerializeField] private UnityEvent onItemPlaced;

    private DayLoopRuntime subscribedRuntime;
    private ItemInteractable placedItem;

    public bool HasPlacedItem => placedItem != null;

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

        return CanAccept(interactor.HeldItem) ? placementPrompt : "Wrong Item";
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

    public void PlaceItem(ItemInteractable item)
    {
        if (item == null)
        {
            return;
        }

        placedItem = item;
        item.PlaceIntoSocket(socketAnchor != null ? socketAnchor : transform);
        onItemPlaced?.Invoke();
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        placedItem = null;
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