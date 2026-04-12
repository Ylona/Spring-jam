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
        private const string ReadGardenSignText = "Read Garden Sign";
        private const string GardenSignSpeakerName = "Garden Sign";
        private const string MeadowOrderClue = "Smallest first, tallest last: snowdrop, crocus, tulip.";
        private const string BunnyIntroLineOne = "You're awake in the same morning too, aren't you?";
        private const string BunnyIntroLineTwo = "I tucked the cherry basket near the old path before the light turns warm.";
        private const string BunnyIntroLineThree = "For the meadow, remember this: " + MeadowOrderClue;
        private const string BunnyReminderLineOne = "There, you remembered.";
        private const string BunnyReminderLineTwo = "Old path early for the basket.";
        private const string BunnyReminderLineThree = "Then wake the meadow the same way: " + MeadowOrderClue;

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

            CreateMeadowClueSign(puzzleRoot.transform, new Vector3(-2.15f, 0f, -0.15f));
            CreateBlossomPetalPickup(puzzleRoot.transform, new Vector3(0f, 0.35f, -0.9f));
            UpdateBunnyHintDialogue();

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

            RemoveCollider(platform);
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

        private static void CreateMeadowClueSign(Transform parent, Vector3 localPosition)
        {
            GameObject signRoot = new GameObject("Meadow Clue Sign");
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.localPosition = localPosition;
            signRoot.transform.localRotation = Quaternion.identity;
            signRoot.transform.localScale = Vector3.one;

            BoxCollider trigger = signRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.85f, 0f);
            trigger.size = new Vector3(1.15f, 1.45f, 0.45f);

            NpcInteractable interactable = signRoot.AddComponent<NpcInteractable>();
            SerializedObject signObject = new SerializedObject(interactable);
            signObject.FindProperty("interactionText").stringValue = ReadGardenSignText;
            signObject.FindProperty("fallbackInteractionText").stringValue = ReadGardenSignText;
            SetStringList(signObject.FindProperty("knowledgeIdsToLearn"));
            SetStringList(signObject.FindProperty("taskIdsToComplete"));
            ConfigureDialogueVariants(
                signObject.FindProperty("dialogueVariants"),
                new DialogueVariantData(
                    sequenceId: "meadow-clue-sign",
                    interactionText: ReadGardenSignText,
                    speakerName: GardenSignSpeakerName,
                    lineBodies: new[]
                    {
                        "Painted garden sign:",
                        MeadowOrderClue,
                    },
                    knowledgeIdsToLearn: System.Array.Empty<string>(),
                    taskIdsToComplete: System.Array.Empty<string>(),
                    requiredKnowledgeIds: System.Array.Empty<string>(),
                    blockedKnowledgeIds: System.Array.Empty<string>(),
                    requiredCompletedTaskIds: System.Array.Empty<string>(),
                    requiredIncompleteTaskIds: System.Array.Empty<string>()));
            signObject.ApplyModifiedPropertiesWithoutUndo();

            CreateSignVisuals(signRoot.transform);
        }

        private static void CreateSignVisuals(Transform parent)
        {
            CreateSignPiece(parent, "post-left", new Vector3(-0.28f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.12f));
            CreateSignPiece(parent, "post-right", new Vector3(0.28f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.12f));
            CreateSignPiece(parent, "board", new Vector3(0f, 0.95f, 0f), new Vector3(0.95f, 0.55f, 0.12f));
            CreateSignPiece(parent, "trim", new Vector3(0f, 1.27f, 0f), new Vector3(1.05f, 0.08f, 0.12f));
        }

        private static void CreateSignPiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = localScale;
            RemoveCollider(piece);
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

        private static void UpdateBunnyHintDialogue()
        {
            GameObject bunny = GameObject.Find("routine-bunny");
            if (bunny == null)
            {
                Debug.LogWarning("Could not find routine-bunny to update meadow hint text.");
                return;
            }

            NpcInteractable interactable = bunny.GetComponent<NpcInteractable>();
            if (interactable == null)
            {
                Debug.LogWarning("routine-bunny is missing its NpcInteractable component.", bunny);
                return;
            }

            SerializedObject bunnyObject = new SerializedObject(interactable);
            bunnyObject.FindProperty("fallbackInteractionText").stringValue = "Learn routines";
            ConfigureDialogueVariants(
                bunnyObject.FindProperty("dialogueVariants"),
                new DialogueVariantData(
                    sequenceId: "bunny-test-intro",
                    interactionText: "Learn routines",
                    speakerName: "Bunny",
                    lineBodies: new[]
                    {
                        BunnyIntroLineOne,
                        BunnyIntroLineTwo,
                        BunnyIntroLineThree,
                    },
                    knowledgeIdsToLearn: new[] { "bunny-loop-hint" },
                    taskIdsToComplete: System.Array.Empty<string>(),
                    requiredKnowledgeIds: System.Array.Empty<string>(),
                    blockedKnowledgeIds: new[] { "bunny-loop-hint" },
                    requiredCompletedTaskIds: System.Array.Empty<string>(),
                    requiredIncompleteTaskIds: System.Array.Empty<string>()),
                new DialogueVariantData(
                    sequenceId: "bunny-test-reminder",
                    interactionText: "Learn routines",
                    speakerName: "Bunny",
                    lineBodies: new[]
                    {
                        BunnyReminderLineOne,
                        BunnyReminderLineTwo,
                        BunnyReminderLineThree,
                    },
                    knowledgeIdsToLearn: System.Array.Empty<string>(),
                    taskIdsToComplete: System.Array.Empty<string>(),
                    requiredKnowledgeIds: new[] { "bunny-loop-hint" },
                    blockedKnowledgeIds: System.Array.Empty<string>(),
                    requiredCompletedTaskIds: System.Array.Empty<string>(),
                    requiredIncompleteTaskIds: System.Array.Empty<string>()));
            bunnyObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDialogueVariants(SerializedProperty variantsProperty, params DialogueVariantData[] variants)
        {
            variantsProperty.arraySize = variants.Length;
            for (int i = 0; i < variants.Length; i++)
            {
                SerializedProperty variantProperty = variantsProperty.GetArrayElementAtIndex(i);
                SerializedProperty conditionsProperty = variantProperty.FindPropertyRelative("conditions");
                SerializedProperty sequenceProperty = variantProperty.FindPropertyRelative("sequence");
                DialogueVariantData variant = variants[i];

                SetStringList(conditionsProperty.FindPropertyRelative("requiredKnowledgeIds"), variant.RequiredKnowledgeIds);
                SetStringList(conditionsProperty.FindPropertyRelative("blockedKnowledgeIds"), variant.BlockedKnowledgeIds);
                SetStringList(conditionsProperty.FindPropertyRelative("requiredCompletedTaskIds"), variant.RequiredCompletedTaskIds);
                SetStringList(conditionsProperty.FindPropertyRelative("requiredIncompleteTaskIds"), variant.RequiredIncompleteTaskIds);

                sequenceProperty.FindPropertyRelative("sequenceId").stringValue = variant.SequenceId;
                sequenceProperty.FindPropertyRelative("interactionText").stringValue = variant.InteractionText;
                sequenceProperty.FindPropertyRelative("speakerName").stringValue = variant.SpeakerName;
                SetDialogueLines(sequenceProperty.FindPropertyRelative("lines"), variant.SpeakerName, variant.LineBodies);
                SetStringList(sequenceProperty.FindPropertyRelative("knowledgeIdsToLearn"), variant.KnowledgeIdsToLearn);
                SetStringList(sequenceProperty.FindPropertyRelative("taskIdsToComplete"), variant.TaskIdsToComplete);
            }
        }

        private static void SetDialogueLines(SerializedProperty linesProperty, string speakerName, IReadOnlyList<string> lineBodies)
        {
            linesProperty.arraySize = lineBodies.Count;
            for (int i = 0; i < lineBodies.Count; i++)
            {
                SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(i);
                lineProperty.FindPropertyRelative("speakerName").stringValue = speakerName;
                lineProperty.FindPropertyRelative("body").stringValue = lineBodies[i];
            }
        }

        private static void SetStringList(SerializedProperty listProperty, params string[] values)
        {
            SetStringList(listProperty, (IReadOnlyList<string>)values);
        }

        private static void SetStringList(SerializedProperty listProperty, IReadOnlyList<string> values)
        {
            listProperty.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                listProperty.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private readonly struct DialogueVariantData
        {
            public DialogueVariantData(
                string sequenceId,
                string interactionText,
                string speakerName,
                IReadOnlyList<string> lineBodies,
                IReadOnlyList<string> knowledgeIdsToLearn,
                IReadOnlyList<string> taskIdsToComplete,
                IReadOnlyList<string> requiredKnowledgeIds,
                IReadOnlyList<string> blockedKnowledgeIds,
                IReadOnlyList<string> requiredCompletedTaskIds,
                IReadOnlyList<string> requiredIncompleteTaskIds)
            {
                SequenceId = sequenceId;
                InteractionText = interactionText;
                SpeakerName = speakerName;
                LineBodies = lineBodies;
                KnowledgeIdsToLearn = knowledgeIdsToLearn;
                TaskIdsToComplete = taskIdsToComplete;
                RequiredKnowledgeIds = requiredKnowledgeIds;
                BlockedKnowledgeIds = blockedKnowledgeIds;
                RequiredCompletedTaskIds = requiredCompletedTaskIds;
                RequiredIncompleteTaskIds = requiredIncompleteTaskIds;
            }

            public string SequenceId { get; }
            public string InteractionText { get; }
            public string SpeakerName { get; }
            public IReadOnlyList<string> LineBodies { get; }
            public IReadOnlyList<string> KnowledgeIdsToLearn { get; }
            public IReadOnlyList<string> TaskIdsToComplete { get; }
            public IReadOnlyList<string> RequiredKnowledgeIds { get; }
            public IReadOnlyList<string> BlockedKnowledgeIds { get; }
            public IReadOnlyList<string> RequiredCompletedTaskIds { get; }
            public IReadOnlyList<string> RequiredIncompleteTaskIds { get; }
        }
    }
}

