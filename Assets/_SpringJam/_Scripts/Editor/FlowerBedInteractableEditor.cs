using UnityEditor;
using UnityEngine;


// Show only the fields that are needed for either sprite mode or color feedback with the flower puzzle
[CustomEditor(typeof(FlowerBedInteractable))]
public class FlowerBedInteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject,
            "useSpriteMode",
            "flowerSpriteRenderer", "failedSprite", "dormantSprite", "activatedSprite", "completedSprite",
            "feedbackRenderers", "dormantColor", "activatedColor", "failedColor", "completedColor",
            "activatedScaleMultiplier", "failedScaleMultiplier", "completedScaleMultiplier");

        SerializedProperty useSpriteProp = serializedObject.FindProperty("useSpriteMode");
        if (useSpriteProp == null)
        {
            EditorGUILayout.HelpBox("useSpriteMode property not found!", MessageType.Error);
            return;
        }
        EditorGUILayout.PropertyField(useSpriteProp);
        bool useSprite = useSpriteProp.boolValue;
        if (useSprite)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flowerSpriteRenderer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dormantSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activatedSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("completedSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("failedSprite"));
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("feedbackRenderers"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dormantColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activatedColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("failedColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("completedColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activatedScaleMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("failedScaleMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("completedScaleMultiplier"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
