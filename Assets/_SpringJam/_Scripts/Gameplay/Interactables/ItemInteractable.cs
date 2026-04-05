using UnityEngine;

public class ItemInteractable : BaseInteractable
{
    [Header("Item")]
    [SerializeField] private string itemPromptText = "Collect";

    public override void Interact()
    {
        Debug.Log("Feels like this item should be in your inventory now. Must be a dream because it's still on the ground.", this);
    }

    public override string GetInteractionText()
    {
        return string.IsNullOrWhiteSpace(itemPromptText) ? base.GetInteractionText() : itemPromptText.Trim();
    }
}
