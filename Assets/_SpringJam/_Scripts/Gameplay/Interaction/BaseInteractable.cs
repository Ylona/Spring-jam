using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionText = "Interact";

    public virtual void Interact(PlayerInteractor interactor)
    {
        Debug.Log("Default interact", this);
    }

    public virtual string GetInteractionText(PlayerInteractor interactor)
    {
        return string.IsNullOrWhiteSpace(interactionText) ? "Interact" : interactionText.Trim();
    }
}
