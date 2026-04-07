using UnityEngine;
using UnityEngine.Events;

public sealed class FlowerBedInteractable : BaseInteractable
{
    [Header("Flower Bed")]
    [SerializeField] private string flowerId = "snowdrop";
    [SerializeField] private string displayName = "Snowdrop";
    [SerializeField] private string activatedInteractionText = "Blooming";
    [SerializeField] private string completedInteractionText = "Bloomed";
    [SerializeField] private FlowerBloomPuzzleController puzzleController;

    [Header("Events")]
    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onRejected;
    [SerializeField] private UnityEvent onReset;

    private bool isActivated;

    public string FlowerId => NormalizeId(flowerId);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? FlowerId : displayName.Trim();
    public bool IsActivated => isActivated;

    private void Reset()
    {
        TryAssignParentController();
    }

    private void OnValidate()
    {
        TryAssignParentController();
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (puzzleController == null)
        {
            Debug.LogWarning($"Flower bed '{DisplayName}' is missing its puzzle controller reference.", this);
            return;
        }

        puzzleController.TryActivateBed(this);
    }

    public override string GetInteractionText(PlayerInteractor interactor)
    {
        if (puzzleController != null && puzzleController.IsCompleted)
        {
            return string.IsNullOrWhiteSpace(completedInteractionText) ? "Bloomed" : completedInteractionText.Trim();
        }

        if (isActivated)
        {
            return string.IsNullOrWhiteSpace(activatedInteractionText) ? "Blooming" : activatedInteractionText.Trim();
        }

        return base.GetInteractionText(interactor);
    }

    internal void SetPuzzleController(FlowerBloomPuzzleController controller)
    {
        if (controller != null)
        {
            puzzleController = controller;
        }
    }

    internal void NotifyActivated()
    {
        if (isActivated)
        {
            return;
        }

        isActivated = true;
        onActivated?.Invoke();
    }

    internal void NotifyRejectedInteraction()
    {
        onRejected?.Invoke();
    }

    internal void ResetState(bool invokeResetEvent)
    {
        isActivated = false;
        if (invokeResetEvent)
        {
            onReset?.Invoke();
        }
    }

    private void TryAssignParentController()
    {
        if (puzzleController == null)
        {
            puzzleController = GetComponentInParent<FlowerBloomPuzzleController>();
        }
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
