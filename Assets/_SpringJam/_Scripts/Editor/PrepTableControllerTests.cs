using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpringJam.Systems.DayLoop;
using UnityEngine;

namespace SpringJam.Tests.EditMode
{
    public sealed class PrepTableControllerTests
    {
        private static readonly string[] RequiredIngredientIds =
        {
            "cherry-basket",
            "honey-jar",
            "mint-bundle",
            "blossom-petals",
        };

        [Test]
        public void EvaluateCompletion_WhenIngredientMissing_DoesNotCompleteMeal()
        {
            TestScenario scenario = CreateScenario();
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            CompletePrerequisiteTasks(scenario.Runtime);

            DayLoopEndContext endingContext = null;
            scenario.Runtime.LoopEnded += context => endingContext = context;

            scenario.PlaceIngredient("cherry-basket");
            scenario.PlaceIngredient("honey-jar");
            scenario.PlaceIngredient("mint-bundle");

            Assert.That(scenario.Controller.HasAllIngredients, Is.False);
            Assert.That(endingContext, Is.Null);
            Assert.That(scenario.Runtime.TryGetTask("cook-spring-meal", out DayLoopTaskSnapshot cookingTask), Is.True);
            Assert.That(cookingTask.IsUnlocked, Is.True);
            Assert.That(cookingTask.IsCompleted, Is.False);

            DestroyScenario(scenario);
        }

        [Test]
        public void EvaluateCompletion_WhenWrongItemPlaced_DoesNotCompleteMeal()
        {
            TestScenario scenario = CreateScenario(allowAnyItemInSockets: true);
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            CompletePrerequisiteTasks(scenario.Runtime);

            DayLoopEndContext endingContext = null;
            scenario.Runtime.LoopEnded += context => endingContext = context;

            scenario.PlaceItemInSocket("winter-stone", "cherry-basket");
            scenario.PlaceIngredient("honey-jar");
            scenario.PlaceIngredient("mint-bundle");
            scenario.PlaceIngredient("blossom-petals");

            Assert.That(scenario.Controller.HasAllIngredients, Is.False);
            Assert.That(endingContext, Is.Null);
            Assert.That(scenario.Runtime.TryGetTask("cook-spring-meal", out DayLoopTaskSnapshot cookingTask), Is.True);
            Assert.That(cookingTask.IsCompleted, Is.False);

            DestroyScenario(scenario);
        }

        [Test]
        public void EvaluateCompletion_WhenAllIngredientsPlacedAndCookingUnlocked_CompletesSuccessfulLoop()
        {
            TestScenario scenario = CreateScenario();
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            CompletePrerequisiteTasks(scenario.Runtime);

            DayLoopEndContext endingContext = null;
            scenario.Runtime.LoopEnded += context => endingContext = context;

            foreach (string ingredientId in RequiredIngredientIds)
            {
                scenario.PlaceIngredient(ingredientId);
            }

            Assert.That(endingContext, Is.Not.Null);
            Assert.That(endingContext.Reason, Is.EqualTo(DayLoopEndReason.SuccessfulLoop));
            Assert.That(scenario.Runtime.ActiveLoopIndex, Is.EqualTo(2));
            Assert.That(scenario.Runtime.CurrentPhase, Is.EqualTo(DayLoopPhase.StartDay));

            DestroyScenario(scenario);
        }

        [Test]
        public void EvaluateCompletion_WhenIngredientsPlacedBeforeCookingUnlock_CompletesAfterPrerequisitesFinish()
        {
            TestScenario scenario = CreateScenario();
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);

            DayLoopEndContext endingContext = null;
            scenario.Runtime.LoopEnded += context => endingContext = context;

            foreach (string ingredientId in RequiredIngredientIds)
            {
                scenario.PlaceIngredient(ingredientId);
            }

            Assert.That(scenario.Controller.HasAllIngredients, Is.True);
            Assert.That(endingContext, Is.Null);
            Assert.That(scenario.Runtime.TryGetTask("cook-spring-meal", out DayLoopTaskSnapshot lockedCookingTask), Is.True);
            Assert.That(lockedCookingTask.IsUnlocked, Is.False);

            CompletePrerequisiteTasks(scenario.Runtime, assertCookingUnlocked: false);

            Assert.That(endingContext, Is.Not.Null);
            Assert.That(endingContext.Reason, Is.EqualTo(DayLoopEndReason.SuccessfulLoop));

            DestroyScenario(scenario);
        }

        private static TestScenario CreateScenario(bool allowAnyItemInSockets = false)
        {
            if (DayLoopRuntime.Instance != null)
            {
                Object.DestroyImmediate(DayLoopRuntime.Instance.gameObject);
            }

            GameObject runtimeRoot = new GameObject("DayLoopRuntime");
            DayLoopRuntime runtime = runtimeRoot.AddComponent<DayLoopRuntime>();
            InitializeRuntime(runtime);

            GameObject tableRoot = new GameObject("Prep Table");
            PrepTableController controller = tableRoot.AddComponent<PrepTableController>();

            Dictionary<string, ItemSocketInteractable> sockets = new Dictionary<string, ItemSocketInteractable>();
            List<PrepTableIngredientRequirement> requirements = new List<PrepTableIngredientRequirement>();
            foreach (string ingredientId in RequiredIngredientIds)
            {
                ItemSocketInteractable socket = CreateSocket(tableRoot.transform, ingredientId, allowAnyItemInSockets);
                sockets[ingredientId] = socket;
                requirements.Add(new PrepTableIngredientRequirement(ingredientId, socket));
            }

            SetPrivateField(controller, "ingredientSockets", requirements);
            InvokePrivateMethod(controller, "OnEnable");
            InvokePrivateMethod(controller, "Start");

            return new TestScenario(runtimeRoot, runtime, tableRoot, controller, sockets, new List<GameObject>());
        }

        private static ItemSocketInteractable CreateSocket(Transform parent, string ingredientId, bool allowAnyItem)
        {
            GameObject socketRoot = new GameObject(ingredientId + " Socket");
            socketRoot.transform.SetParent(parent, false);

            GameObject anchorRoot = new GameObject("Anchor");
            anchorRoot.transform.SetParent(socketRoot.transform, false);

            ItemSocketInteractable socket = socketRoot.AddComponent<ItemSocketInteractable>();
            SetPrivateField(socket, "socketAnchor", anchorRoot.transform);
            SetPrivateField(socket, "acceptedItemIds", allowAnyItem ? new List<string>() : new List<string> { ingredientId });
            SetPrivateField(socket, "placementPrompt", "Place " + ingredientId);
            SetPrivateField(socket, "requiredCompletedTaskIds", new List<string>());
            InvokePrivateMethod(socket, "Awake");
            InvokePrivateMethod(socket, "OnEnable");
            InvokePrivateMethod(socket, "Start");
            return socket;
        }

        private static ItemInteractable CreateItem(string itemId)
        {
            GameObject itemRoot = new GameObject(itemId);
            ItemInteractable item = itemRoot.AddComponent<ItemInteractable>();
            SetPrivateField(item, "itemId", itemId);
            SetPrivateField(item, "displayName", itemId);
            SetPrivateField(item, "pickupPrompt", "Pick Up " + itemId);
            InvokePrivateMethod(item, "Awake");
            InvokePrivateMethod(item, "OnEnable");
            return item;
        }

        private static void CompletePrerequisiteTasks(DayLoopRuntime runtime, bool assertCookingUnlocked = true)
        {
            Assert.That(runtime.TryCompleteTask("bloom-flowers"), Is.True);
            Assert.That(runtime.TryCompleteTask("guide-bees"), Is.True);
            Assert.That(runtime.TryCompleteTask("learn-routines"), Is.True);

            if (!assertCookingUnlocked)
            {
                return;
            }

            Assert.That(runtime.TryGetTask("cook-spring-meal", out DayLoopTaskSnapshot cookingTask), Is.True);
            Assert.That(cookingTask.IsUnlocked, Is.True);
        }

        private static void InitializeRuntime(DayLoopRuntime runtime)
        {
            InvokePrivateMethod(runtime, "Awake");
            InvokePrivateMethod(runtime, "Start");
            Assert.That(DayLoopRuntime.Instance, Is.SameAs(runtime));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method '{methodName}' on {target.GetType().Name}.");
            method.Invoke(target, arguments.Length == 0 ? null : arguments);
        }

        private static void DestroyScenario(TestScenario scenario)
        {
            foreach (GameObject itemRoot in scenario.ItemRoots)
            {
                if (itemRoot != null)
                {
                    Object.DestroyImmediate(itemRoot);
                }
            }

            Object.DestroyImmediate(scenario.TableRoot);
            Object.DestroyImmediate(scenario.RuntimeRoot);
        }

        private readonly struct TestScenario
        {
            private readonly Dictionary<string, ItemSocketInteractable> sockets;
            private readonly List<GameObject> itemRoots;

            public TestScenario(
                GameObject runtimeRoot,
                DayLoopRuntime runtime,
                GameObject tableRoot,
                PrepTableController controller,
                Dictionary<string, ItemSocketInteractable> sockets,
                List<GameObject> itemRoots)
            {
                RuntimeRoot = runtimeRoot;
                Runtime = runtime;
                TableRoot = tableRoot;
                Controller = controller;
                this.sockets = sockets;
                this.itemRoots = itemRoots;
            }

            public GameObject RuntimeRoot { get; }
            public DayLoopRuntime Runtime { get; }
            public GameObject TableRoot { get; }
            public PrepTableController Controller { get; }
            public IReadOnlyList<GameObject> ItemRoots => itemRoots;

            public void PlaceIngredient(string ingredientId)
            {
                PlaceItemInSocket(ingredientId, ingredientId);
            }

            public void PlaceItemInSocket(string itemId, string socketIngredientId)
            {
                Assert.That(sockets.TryGetValue(socketIngredientId, out ItemSocketInteractable socket), Is.True);
                ItemInteractable item = CreateItem(itemId);
                itemRoots.Add(item.gameObject);
                Assert.That(socket.PlaceItem(item), Is.True);
            }
        }
    }
}
