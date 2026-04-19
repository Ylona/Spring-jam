using System.Collections.Generic;
using SpringJam.Dialogue;
using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

public class NpcInteractable : BaseInteractable
{
    [Header("NPC Dialogue")]
    [SerializeField] private string fallbackInteractionText = "Talk";
    [SerializeField] private string dialogueJsonFile = string.Empty;
    [SerializeField] private List<ConditionalDialogueSequenceDefinition> dialogueVariants = new List<ConditionalDialogueSequenceDefinition>();
    [SerializeField] private UnityEvent onNpcTalkedTo;

    private NPCWanderer wanderer;

    private void Awake()
    {
        wanderer = GetComponent<NPCWanderer>();
        LoadDialogueFromJson();
    }

    [ContextMenu("Save Dialogue To JSON")]
    private void SaveDialogueToJson()
    {
        if (string.IsNullOrWhiteSpace(dialogueJsonFile))
        {
            Debug.LogWarning("Dialogue Json File is empty — fill in a filename first.", this);
            return;
        }

#if UNITY_EDITOR
        NpcDialogueData data = new NpcDialogueData { variants = dialogueVariants };
        string json = JsonUtility.ToJson(data, true);
        string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources", "Dialogue", dialogueJsonFile.Trim() + ".json");
        System.IO.File.WriteAllText(path, json);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"Saved dialogue to '{path}'.", this);
#endif
    }

    [ContextMenu("Load Dialogue From JSON")]
    public void LoadDialogueFromJson()
    {
        if (string.IsNullOrWhiteSpace(dialogueJsonFile))
            return;

        string resourcePath = $"Dialogue/{dialogueJsonFile.Trim()}";
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogWarning($"Dialogue JSON '{resourcePath}' not found in Resources/Dialogue/. Enter only the filename without extension.", this);
            return;
        }

        NpcDialogueData data = JsonUtility.FromJson<NpcDialogueData>(asset.text);
        if (data?.variants != null && data.variants.Count > 0)
        {
            dialogueVariants = data.variants;
            Debug.Log($"Loaded {data.variants.Count} dialogue variant(s) from '{resourcePath}'.", this);
        }
    }

    public override void Interact(PlayerInteractor interactor)
    {
        DialogueSequenceDefinition sequence = SelectSequence();
        if (sequence == null)
        {
            Debug.LogWarning($"No dialogue configured for {name}.", this);
            onNpcTalkedTo?.Invoke();
            return;
        }

        if (wanderer != null) wanderer.StopWandering();

        void OnResume() { if (wanderer != null) wanderer.ResumeWandering(); }
        DialogueConversation conversation = sequence.CreateConversation(
            onCompleted: () => { OnResume(); CompleteSequence(sequence); },
            onCancelled: OnResume);
        if (conversation == null)
        {
            if (wanderer != null) wanderer.ResumeWandering();
            Debug.LogWarning($"Dialogue sequence on {name} has no lines.", this);
            return;
        }

        DialogueRuntimeController.TryStartConversation(conversation);
    }

    public override string GetInteractionText(PlayerInteractor interactor)
    {
        DialogueSequenceDefinition sequence = SelectSequence();
        if (sequence != null && !string.IsNullOrWhiteSpace(sequence.InteractionText))
        {
            return sequence.InteractionText;
        }

        return string.IsNullOrWhiteSpace(fallbackInteractionText)
            ? base.GetInteractionText(interactor)
            : fallbackInteractionText.Trim();
    }

    private void CompleteSequence(DialogueSequenceDefinition sequence)
    {
        sequence.ApplyProgressionEffects(DayLoopRuntime.Instance);
        ApplyInteractionProgression();
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
