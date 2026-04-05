using UnityEngine;
using UnityEngine.Events;
public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    public virtual void Interact()
    {
        Debug.Log("Default interact");
    }

    public virtual string GetInteractionText()
    {
        return "Interact";
    }
}