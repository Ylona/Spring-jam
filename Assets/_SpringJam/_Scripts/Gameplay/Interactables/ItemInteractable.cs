using UnityEngine;
using UnityEngine.Events;

public class ItemInteractable : BaseInteractable
{
    public override void Interact()
    {
        Debug.Log("Feels like this item should be in your inventory now... Must be a dream because it's still on the ground");
    }
}