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

            CreateBlossomPetalPickup(puzzleRoot.transform, new Vector3(0f, 0.35f, -0.9f));

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

        private static void CreateBlossomPetalPickup(Transform parent, Vector3 localPosition)
        {
            GameObject petals = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            petals.name = "Blossom Petals";
            petals.transform.SetParent(parent, false);
            petals.transform.localPosition = localPosition;
            petals.transform.localRotation = Quaternion.identity;
            petals.transform.localScale = new Vector3(0.42f, 0.2f, 0.42f);

            ItemInteractable interactable = petals.AddComponent<ItemInteractable>();
            SerializedObject itemObject = new SerializedObject(interactable);
            itemObject.FindProperty("interactionText").stringValue = "Collect Blossom Petals";
            itemObject.FindProperty("itemId").stringValue = "blossom-petals";
            itemObject.FindProperty("displayName").stringValue = "Blossom Petals";
            itemObject.FindProperty("pickupPrompt").stringValue = "Collect Blossom Petals";
            itemObject.FindProperty("lockedPickupPrompt").stringValue = "Bloom Meadow First";
            itemObject.FindProperty("lockedPickupMessage").stringValue = "The petals are not ready to gather yet.";

            SerializedProperty requiredTasksProperty = itemObject.FindProperty("requiredCompletedTaskIds");
            requiredTasksProperty.arraySize = 1;
            requiredTasksProperty.GetArrayElementAtIndex(0).stringValue = "bloom-flowers";
            itemObject.ApplyModifiedPropertiesWithoutUndo();

            CreatePetalCluster(petals.transform);
        }

        private static void CreatePetalCluster(Transform parent)
        {
            CreatePetal(parent, new Vector3(-0.18f, 0.08f, 0f), new Vector3(0.24f, 0.12f, 0.18f));
            CreatePetal(parent, new Vector3(0.18f, 0.08f, 0f), new Vector3(0.24f, 0.12f, 0.18f));
            CreatePetal(parent, new Vector3(0f, 0.12f, 0.14f), new Vector3(0.22f, 0.1f, 0.16f));
        }

        private static void CreatePetal(Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            petal.name = "petal";
            petal.transform.SetParent(parent, false);
            petal.transform.localPosition = localPosition;
            petal.transform.localRotation = Quaternion.identity;
            petal.transform.localScale = localScale;
            RemoveCollider(petal);
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
