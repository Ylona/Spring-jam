using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NpcInteractable))]
public class NpcInteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        NpcInteractable npc = (NpcInteractable)target;

        if (GUILayout.Button("Reload Dialogue From JSON"))
        {
            npc.LoadDialogueFromJson();
            EditorUtility.SetDirty(npc);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reload All NPCs In Scene"))
        {
            ReloadAllNpcs();
        }
    }

    private static void ReloadAllNpcs()
    {
        NpcInteractable[] all = FindObjectsByType<NpcInteractable>(FindObjectsSortMode.None);
        foreach (NpcInteractable npc in all)
        {
            npc.LoadDialogueFromJson();
            EditorUtility.SetDirty(npc);
        }

        Debug.Log($"Reloaded dialogue JSON for {all.Length} NPC(s).");
    }
}
