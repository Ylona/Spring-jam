using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpringJam.Systems.DayLoop;
using UnityEngine;

namespace SpringJam.Tests.EditMode
{
    public sealed class ItemInteractableTests
    {
        [Test]
        public void GetInteractionText_WhenRequiredTaskIncomplete_ShowsLockedPrompt()
        {
            TestScenario scenario = CreateScenario();

            string prompt = scenario.Item.GetInteractionText(scenario.Interactor);

            Assert.That(prompt, Is.EqualTo("Bloom Meadow First"));

            DestroyScenario(scenario);
        }

        [Test]
        public void Interact_WhenRequiredTaskIncomplete_DoesNotPickUpItem()
        {
            TestScenario scenario = CreateScenario();

            scenario.Item.Interact(scenario.Interactor);

            Assert.That(scenario.Item.IsHeld, Is.False);
            Assert.That(scenario.Interactor.HeldItem, Is.Null);

            DestroyScenario(scenario);
        }

        [Test]
        public void Interact_WhenRequiredTaskComplete_AllowsPickup()
        {
            TestScenario scenario = CreateScenario();
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            Assert.That(scenario.Runtime.TryCompleteTask("bloom-flowers"), Is.True);

            scenario.Item.Interact(scenario.Interactor);

            Assert.That(scenario.Item.IsHeld, Is.True);
            Assert.That(scenario.Interactor.HeldItem, Is.SameAs(scenario.Item));
            Assert.That(scenario.Item.GetInteractionText(scenario.Interactor), Is.EqualTo(string.Empty));

            DestroyScenario(scenario);
        }

        private static TestScenario CreateScenario()
        {
            if (DayLoopRuntime.Instance != null)
            {
                Object.DestroyImmediate(DayLoopRuntime.Instance.gameObject);
            }

            GameObject runtimeRoot = new GameObject("DayLoopRuntime");
            DayLoopRuntime runtime = runtimeRoot.AddComponent<DayLoopRuntime>();
            InitializeRuntime(runtime);

            GameObject playerRoot = new GameObject("Player");
            PlayerInteractor interactor = playerRoot.AddComponent<PlayerInteractor>();

            GameObject itemRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            itemRoot.name = "Blossom Petals";
            ItemInteractable item = itemRoot.AddComponent<ItemInteractable>();
            InvokePrivateMethod(item, "Awake");
            SetPrivateField(item, "itemId", "blossom-petals");
            SetPrivateField(item, "displayName", "Blossom Petals");
            SetPrivateField(item, "pickupPrompt", "Collect Blossom Petals");
            SetPrivateField(item, "lockedPickupPrompt", "Bloom Meadow First");
            SetPrivateField(item, "lockedPickupMessage", "The petals are not ready to gather yet.");
            SetPrivateField(item, "requiredCompletedTaskIds", new List<string> { "bloom-flowers" });

            return new TestScenario(runtimeRoot, runtime, playerRoot, interactor, itemRoot, item);
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

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method '{methodName}' on {target.GetType().Name}.");
            method.Invoke(target, null);
        }

        private static void DestroyScenario(TestScenario scenario)
        {
            Object.DestroyImmediate(scenario.ItemRoot);
            Object.DestroyImmediate(scenario.PlayerRoot);
            Object.DestroyImmediate(scenario.RuntimeRoot);
        }

        private readonly struct TestScenario
        {
            public TestScenario(
                GameObject runtimeRoot,
                DayLoopRuntime runtime,
                GameObject playerRoot,
                PlayerInteractor interactor,
                GameObject itemRoot,
                ItemInteractable item)
            {
                RuntimeRoot = runtimeRoot;
                Runtime = runtime;
                PlayerRoot = playerRoot;
                Interactor = interactor;
                ItemRoot = itemRoot;
                Item = item;
            }

            public GameObject RuntimeRoot { get; }
            public DayLoopRuntime Runtime { get; }
            public GameObject PlayerRoot { get; }
            public PlayerInteractor Interactor { get; }
            public GameObject ItemRoot { get; }
            public ItemInteractable Item { get; }
        }
    }
}
