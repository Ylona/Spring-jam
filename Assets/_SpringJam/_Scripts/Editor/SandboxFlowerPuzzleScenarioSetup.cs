using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpringJam.EditorTools
{
    public static class SandboxFlowerPuzzleScenarioSetup
    {
        private const string ScenePath = "Assets/_SpringJam/Scenes/Sandbox.unity";

        [MenuItem("Tools/Spring Jam/Setup Sandbox Flower Puzzle")]
        public static void ApplyToSandboxScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject doorRoot = GameObject.Find("Door");
            if (doorRoot == null)
            {
                doorRoot = new GameObject("Door");
            }

            DestroyChildIfPresent(doorRoot.transform, "bloom-station");
            DestroyChildIfPresent(doorRoot.transform, "flower-puzzle-test");

            GameObject puzzleRoot = new GameObject("flower-puzzle-test");
            puzzleRoot.transform.SetParent(doorRoot.transform, false);
            puzzleRoot.transform.localPosition = new Vector3(3.4f, 0f, -0.55f);
            puzzleRoot.transform.localRotation = Quaternion.identity;
            puzzleRoot.transform.localScale = Vector3.one;

            CreatePlatform(puzzleRoot.transform);

            FlowerBloomPuzzleController controller = puzzleRoot.AddComponent<FlowerBloomPuzzleController>();

            List<FlowerBedInteractable> beds = new List<FlowerBedInteractable>
            {
                CreateFlowerBed(puzzleRoot.transform, controller, "snowdrop", "Snowdrop", new Vector3(-1.35f, 0.45f, 0f), new Vector3(0.65f, 0.9f, 0.65f), 0.33f),
                CreateFlowerBed(puzzleRoot.transform, controller, "crocus", "Crocus", new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 1.1f, 0.8f), 0.42f),
                CreateFlowerBed(puzzleRoot.transform, controller, "tulip", "Tulip", new Vector3(1.35f, 0.7f, 0f), new Vector3(0.95f, 1.4f, 0.95f), 0.52f),
            };

            SerializedObject controllerObject = new SerializedObject(controller);
            SerializedProperty orderedBedsProperty = controllerObject.FindProperty("orderedFlowerBeds");
            orderedBedsProperty.arraySize = beds.Count;
            for (int i = 0; i < beds.Count; i++)
            {
                orderedBedsProperty.GetArrayElementAtIndex(i).objectReferenceValue = beds[i];
            }

            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Sandbox flower puzzle scenario applied.");
        }

        private static void DestroyChildIfPresent(Transform parent, string childName)
        {
            Transform existingChild = parent.Find(childName);
            if (existingChild != null)
            {
                Object.DestroyImmediate(existingChild.gameObject);
            }
        }

        private static void CreatePlatform(Transform parent)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "platform";
            platform.transform.SetParent(parent, false);
            platform.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            platform.transform.localScale = new Vector3(4.8f, 0.1f, 1.8f);

            Collider collider = platform.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static FlowerBedInteractable CreateFlowerBed(
            Transform parent,
            FlowerBloomPuzzleController controller,
            string flowerId,
            string displayName,
            Vector3 localPosition,
            Vector3 localScale,
            float blossomScale)
        {
            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = displayName + " Bed";
            bed.transform.SetParent(parent, false);
            bed.transform.localPosition = localPosition;
            bed.transform.localRotation = Quaternion.identity;
            bed.transform.localScale = localScale;

            BoxCollider collider = bed.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            FlowerBedInteractable interactable = bed.AddComponent<FlowerBedInteractable>();
            SerializedObject interactableObject = new SerializedObject(interactable);
            interactableObject.FindProperty("interactionText").stringValue = "Bloom " + displayName;
            interactableObject.FindProperty("flowerId").stringValue = flowerId;
            interactableObject.FindProperty("displayName").stringValue = displayName;
            interactableObject.FindProperty("activatedInteractionText").stringValue = displayName + " Blooming";
            interactableObject.FindProperty("completedInteractionText").stringValue = displayName + " Bloomed";
            interactableObject.FindProperty("puzzleController").objectReferenceValue = controller;
            interactableObject.ApplyModifiedPropertiesWithoutUndo();

            CreateStemAndBloom(bed.transform, blossomScale);
            return interactable;
        }

        private static void CreateStemAndBloom(Transform parent, float blossomScale)
        {
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stem.name = "stem";
            stem.transform.SetParent(parent, false);
            stem.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            stem.transform.localScale = new Vector3(0.16f, 0.75f, 0.16f);
            RemoveCollider(stem);

            GameObject blossom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blossom.name = "blossom";
            blossom.transform.SetParent(parent, false);
            blossom.transform.localPosition = new Vector3(0f, 1f, 0f);
            blossom.transform.localScale = Vector3.one * blossomScale;
            RemoveCollider(blossom);
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }
    }
}
