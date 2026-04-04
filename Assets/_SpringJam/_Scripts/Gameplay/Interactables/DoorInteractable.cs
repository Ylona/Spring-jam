using UnityEngine;
using UnityEngine.Events;

public class DoorInteractable : BaseInteractable
{
    [Header("Door Event")]
    [SerializeField] private UnityEvent onDoorUsed;

    public override void Interact()
    {
        Debug.Log("This door would definitly open, if I actually made that... ");
        onDoorUsed?.Invoke();
    }
}