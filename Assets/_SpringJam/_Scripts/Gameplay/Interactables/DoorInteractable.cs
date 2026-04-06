using UnityEngine;
using UnityEngine.Events;

public class DoorInteractable : BaseInteractable
{
    [Header("Door")]
    [SerializeField] private string doorPromptText = "Open";
    [SerializeField] private UnityEvent onDoorUsed;

    public override void Interact(PlayerInteractor interactor)
    {
        Debug.Log("This door would definitely open, if I actually made that...", this);
        onDoorUsed?.Invoke();
    }

    public override string GetInteractionText(PlayerInteractor interactor)
    {
        return string.IsNullOrWhiteSpace(doorPromptText)
            ? base.GetInteractionText(interactor)
            : doorPromptText.Trim();
    }
}
