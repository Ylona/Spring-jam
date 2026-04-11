using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionText = "Interact";
    [Header("Progression")]
    [SerializeField] private List<string> knowledgeIdsToLearn = new List<string>();
    [SerializeField] private List<string> taskIdsToComplete = new List<string>();

    public virtual void Interact(PlayerInteractor interactor)
    {
        Debug.Log("Default interact", this);
    }

    public virtual string GetInteractionText(PlayerInteractor interactor)
    {
        return string.IsNullOrWhiteSpace(interactionText) ? "Interact" : interactionText.Trim();
    }

    protected void ApplyInteractionProgression()
    {
        DayLoopProgressionApplier.Apply(DayLoopRuntime.Instance, knowledgeIdsToLearn, taskIdsToComplete);
    }
}
