using UnityEngine;
using UnityEngine.Events;

public class NpcInteractable : BaseInteractable
{
    [Header("NPC Event")]
    [SerializeField] private UnityEvent onNpcTalkedTo;

    public override void Interact()
    {
        Debug.Log("Hello there");
        onNpcTalkedTo?.Invoke();
    }

}