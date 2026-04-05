using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionText = "Interact";

    public virtual void Interact()
    {
        Debug.Log("Default interact", this);
    }

    public virtual string GetInteractionText()
    {
        return string.IsNullOrWhiteSpace(interactionText) ? "Interact" : interactionText.Trim();
    }
}
