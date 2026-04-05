using System.Collections.Generic;
using SpringJam.Dialogue;
using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

public class NpcInteractable : BaseInteractable
{
    [Header("NPC Dialogue")]
    [SerializeField] private string fallbackInteractionText = "Talk";
    [SerializeField] private List<ConditionalDialogueSequenceDefinition> dialogueVariants = new List<ConditionalDialogueSequenceDefinition>();
    [SerializeField] private UnityEvent onNpcTalkedTo;

    public override void Interact()
    {
        DialogueSequenceDefinition sequence = SelectSequence();
        if (sequence == null)
        {
            Debug.LogWarning($"No dialogue configured for {name}.", this);
            onNpcTalkedTo?.Invoke();
            return;
        }

        DialogueConversation conversation = sequence.CreateConversation(() => CompleteSequence(sequence));
        if (conversation == null)
        {
            Debug.LogWarning($"Dialogue sequence on {name} has no lines.", this);
            return;
        }

        DialogueRuntimeController.TryStartConversation(conversation);
    }

    public override string GetInteractionText()
    {
        DialogueSequenceDefinition sequence = SelectSequence();
        if (sequence != null && !string.IsNullOrWhiteSpace(sequence.InteractionText))
        {
            return sequence.InteractionText;
        }

        return string.IsNullOrWhiteSpace(fallbackInteractionText) ? base.GetInteractionText() : fallbackInteractionText.Trim();
    }

    private void CompleteSequence(DialogueSequenceDefinition sequence)
    {
        sequence.ApplyProgressionEffects(DayLoopRuntime.Instance);
        onNpcTalkedTo?.Invoke();
    }

    private DialogueSequenceDefinition SelectSequence()
    {
        DialogueProgressSnapshot progress = DialogueProgressSnapshot.FromRuntime(DayLoopRuntime.Instance);

        foreach (ConditionalDialogueSequenceDefinition variant in dialogueVariants)
        {
            if (variant != null && variant.Matches(progress))
            {
                return variant.Sequence;
            }
        }

        return null;
    }
}
