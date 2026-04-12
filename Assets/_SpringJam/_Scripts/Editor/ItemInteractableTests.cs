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

        [Test]
        public void RestartLoop_AfterPickup_RelocksPickupAndClearsHeldState()
        {
            TestScenario scenario = CreateScenario();
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            Assert.That(scenario.Runtime.TryCompleteTask("bloom-flowers"), Is.True);

            scenario.Item.Interact(scenario.Interactor);
            Assert.That(scenario.Item.IsHeld, Is.True);

            scenario.Runtime.RestartLoop();

            Assert.That(scenario.Runtime.TryGetTask("bloom-flowers", out DayLoopTaskSnapshot taskSnapshot), Is.True);
            Assert.That(taskSnapshot.IsCompleted, Is.False);
            Assert.That(scenario.Item.IsHeld, Is.False);
            Assert.That(scenario.Interactor.HeldItem, Is.Null);
            Assert.That(scenario.Item.GetInteractionText(scenario.Interactor), Is.EqualTo("Bloom Meadow First"));

            DestroyScenario(scenario);
        }

        [Test]
        public void LoopStarted_AfterDestinationPlacement_RestoresLurePotToMeadowStand()
        {
            TestScenario scenario = CreateScenario();
            SetPrivateField(scenario.Item, "itemId", "lure-flower-pot");

            SocketScenario meadowStand = CreateSocketScenario("Meadow Lure Pot Stand", scenario.Item);
            SocketScenario greenhouseStand = CreateSocketScenario("Greenhouse Lure Pot Stand", null);

            Assert.That(meadowStand.Socket.HasPlacedItem, Is.True);
            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.False);
            Assert.That(scenario.Item.transform.parent, Is.SameAs(meadowStand.Anchor));

            Assert.That(greenhouseStand.Socket.PlaceItem(scenario.Item), Is.True);

            Assert.That(meadowStand.Socket.HasPlacedItem, Is.False);
            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.True);
            Assert.That(scenario.Item.transform.parent, Is.SameAs(greenhouseStand.Anchor));

            InvokePrivateMethod(meadowStand.Socket, "HandleLoopStarted", (object)null);
            InvokePrivateMethod(scenario.Item, "HandleLoopStarted", (object)null);
            InvokePrivateMethod(greenhouseStand.Socket, "HandleLoopStarted", (object)null);

            Assert.That(meadowStand.Socket.HasPlacedItem, Is.True);
            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.False);
            Assert.That(scenario.Item.IsPlaced, Is.True);
            Assert.That(scenario.Item.transform.parent, Is.SameAs(meadowStand.Anchor));

            DestroySocketScenario(greenhouseStand);
            DestroySocketScenario(meadowStand);
            DestroyScenario(scenario);
        }

        [Test]
        public void Interact_WhenRequiredSocketTaskIncomplete_DoesNotPlaceHeldItem()
        {
            TestScenario scenario = CreateScenario();
            SetPrivateField(scenario.Item, "itemId", "lure-flower-pot");
            Assert.That(scenario.Interactor.TryPickUpItem(scenario.Item), Is.True);

            SocketScenario greenhouseStand = CreateSocketScenario(
                "Greenhouse Lure Pot Stand",
                null,
                new List<string> { "bloom-flowers" },
                null,
                "guide-bees");

            Assert.That(greenhouseStand.Socket.GetInteractionText(scenario.Interactor), Is.EqualTo("Bloom Meadow First"));

            greenhouseStand.Socket.Interact(scenario.Interactor);

            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.False);
            Assert.That(scenario.Item.IsHeld, Is.True);
            Assert.That(scenario.Interactor.HeldItem, Is.SameAs(scenario.Item));
            Assert.That(scenario.Runtime.TryGetTask("guide-bees", out DayLoopTaskSnapshot taskSnapshot), Is.True);
            Assert.That(taskSnapshot.IsCompleted, Is.False);

            DestroySocketScenario(greenhouseStand);
            DestroyScenario(scenario);
        }

        [Test]
        public void Interact_WhenBeeMovementRequiredButMoverMissing_DoesNotCompleteGuideBees()
        {
            TestScenario scenario = CreateScenario();
            SetPrivateField(scenario.Item, "itemId", "lure-flower-pot");
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            Assert.That(scenario.Runtime.TryCompleteTask("bloom-flowers"), Is.True);
            Assert.That(scenario.Interactor.TryPickUpItem(scenario.Item), Is.True);

            SocketScenario greenhouseStand = CreateSocketScenario(
                "Greenhouse Lure Pot Stand",
                null,
                new List<string> { "bloom-flowers" },
                null,
                "guide-bees");
            SetPrivateField(greenhouseStand.Socket, "moveBeeSwarmOnPlacement", true);

            greenhouseStand.Socket.Interact(scenario.Interactor);

            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.True);
            Assert.That(scenario.Item.IsPlaced, Is.True);
            Assert.That(scenario.Item.IsHeld, Is.False);
            Assert.That(scenario.Interactor.HeldItem, Is.Null);
            Assert.That(scenario.Runtime.TryGetTask("guide-bees", out DayLoopTaskSnapshot taskSnapshot), Is.True);
            Assert.That(taskSnapshot.IsCompleted, Is.False);

            DestroySocketScenario(greenhouseStand);
            DestroyScenario(scenario);
        }

        [Test]
        public void Interact_WhenRequiredSocketTaskComplete_PlacesHeldItem()
        {
            TestScenario scenario = CreateScenario();
            SetPrivateField(scenario.Item, "itemId", "lure-flower-pot");
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            Assert.That(scenario.Runtime.TryCompleteTask("bloom-flowers"), Is.True);
            Assert.That(scenario.Interactor.TryPickUpItem(scenario.Item), Is.True);

            SocketScenario greenhouseStand = CreateSocketScenario(
                "Greenhouse Lure Pot Stand",
                null,
                new List<string> { "bloom-flowers" });

            Assert.That(greenhouseStand.Socket.GetInteractionText(scenario.Interactor), Is.EqualTo("Place Lure Pot"));

            greenhouseStand.Socket.Interact(scenario.Interactor);

            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.True);
            Assert.That(scenario.Item.IsPlaced, Is.True);
            Assert.That(scenario.Item.IsHeld, Is.False);
            Assert.That(scenario.Interactor.HeldItem, Is.Null);
            Assert.That(scenario.Item.transform.parent, Is.SameAs(greenhouseStand.Anchor));

            DestroySocketScenario(greenhouseStand);
            DestroyScenario(scenario);
        }

        [Test]
        public void Interact_WhenRequiredSocketTaskComplete_MovesBeeSwarmToGreenhouseAnchor()
        {
            TestScenario scenario = CreateScenario();
            SetPrivateField(scenario.Item, "itemId", "lure-flower-pot");
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            Assert.That(scenario.Runtime.TryCompleteTask("bloom-flowers"), Is.True);
            Assert.That(scenario.Interactor.TryPickUpItem(scenario.Item), Is.True);

            GameObject meadowAnchorRoot = new GameObject("Bee Meadow Anchor");
            meadowAnchorRoot.transform.position = new Vector3(1f, 0.5f, 2f);
            GameObject greenhouseAnchorRoot = new GameObject("Bee Greenhouse Anchor");
            greenhouseAnchorRoot.transform.position = new Vector3(4f, 0.5f, 5f);
            GameObject swarmRoot = new GameObject("Bee Swarm");
            BeeSwarmAnchorMover swarmMover = swarmRoot.AddComponent<BeeSwarmAnchorMover>();
            SetPrivateField(swarmMover, "meadowAnchor", meadowAnchorRoot.transform);
            SetPrivateField(swarmMover, "greenhouseAnchor", greenhouseAnchorRoot.transform);
            InvokePrivateMethod(swarmMover, "Awake");

            SocketScenario greenhouseStand = CreateSocketScenario(
                "Greenhouse Lure Pot Stand",
                null,
                new List<string> { "bloom-flowers" },
                swarmMover);

            Assert.That(swarmRoot.transform.position, Is.EqualTo(meadowAnchorRoot.transform.position));

            greenhouseStand.Socket.Interact(scenario.Interactor);

            Assert.That(swarmMover.IsAtGreenhouse, Is.True);
            Assert.That(swarmRoot.transform.position, Is.EqualTo(greenhouseAnchorRoot.transform.position));

            DestroySocketScenario(greenhouseStand);
            Object.DestroyImmediate(swarmRoot);
            Object.DestroyImmediate(greenhouseAnchorRoot);
            Object.DestroyImmediate(meadowAnchorRoot);
            DestroyScenario(scenario);
        }

        [Test]
        public void Interact_WhenBeeSwarmRelocates_CompletesGuideBeesAndUnlocksBeeRewards()
        {
            TestScenario scenario = CreateScenario();
            SetPrivateField(scenario.Item, "itemId", "lure-flower-pot");
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            Assert.That(scenario.Runtime.TryCompleteTask("bloom-flowers"), Is.True);
            Assert.That(scenario.Interactor.TryPickUpItem(scenario.Item), Is.True);

            GameObject meadowAnchorRoot = new GameObject("Bee Meadow Anchor");
            GameObject greenhouseAnchorRoot = new GameObject("Bee Greenhouse Anchor");
            GameObject swarmRoot = new GameObject("Bee Swarm");
            BeeSwarmAnchorMover swarmMover = swarmRoot.AddComponent<BeeSwarmAnchorMover>();
            SetPrivateField(swarmMover, "meadowAnchor", meadowAnchorRoot.transform);
            SetPrivateField(swarmMover, "greenhouseAnchor", greenhouseAnchorRoot.transform);
            InvokePrivateMethod(swarmMover, "Awake");

            GameObject mintPatchRoot = new GameObject("Mint Patch");
            ItemInteractable mintPatch = mintPatchRoot.AddComponent<ItemInteractable>();
            SetPrivateField(mintPatch, "itemId", "mint-bundle");
            SetPrivateField(mintPatch, "displayName", "Mint Bundle");
            SetPrivateField(mintPatch, "pickupPrompt", "Harvest Mint");
            SetPrivateField(mintPatch, "lockedPickupPrompt", "Pollinate Mint First");
            SetPrivateField(mintPatch, "lockedPickupMessage", "The mint patch is still dormant.");
            SetPrivateField(mintPatch, "requiredCompletedTaskIds", new List<string> { "guide-bees" });
            InvokePrivateMethod(mintPatch, "Awake");
            InvokePrivateMethod(mintPatch, "OnEnable");

            GameObject honeyShelfRoot = new GameObject("Mara Honey Shelf");
            ItemInteractable honeyShelf = honeyShelfRoot.AddComponent<ItemInteractable>();
            SetPrivateField(honeyShelf, "itemId", "honey-jar");
            SetPrivateField(honeyShelf, "displayName", "Honey Jar");
            SetPrivateField(honeyShelf, "pickupPrompt", "Take Honey Jar");
            SetPrivateField(honeyShelf, "lockedPickupPrompt", "Wait For Mara");
            SetPrivateField(honeyShelf, "lockedPickupMessage", "Mara keeps the honey shelf closed until the bees reach the greenhouse.");
            SetPrivateField(honeyShelf, "requiredCompletedTaskIds", new List<string> { "guide-bees" });
            InvokePrivateMethod(honeyShelf, "Awake");
            InvokePrivateMethod(honeyShelf, "OnEnable");

            SocketScenario greenhouseStand = CreateSocketScenario(
                "Greenhouse Lure Pot Stand",
                null,
                new List<string> { "bloom-flowers" },
                swarmMover,
                "guide-bees");

            Assert.That(mintPatch.GetInteractionText(scenario.Interactor), Is.EqualTo("Pollinate Mint First"));
            Assert.That(honeyShelf.GetInteractionText(scenario.Interactor), Is.EqualTo("Wait For Mara"));

            greenhouseStand.Socket.Interact(scenario.Interactor);

            Assert.That(scenario.Runtime.TryGetTask("guide-bees", out DayLoopTaskSnapshot taskSnapshot), Is.True);
            Assert.That(taskSnapshot.IsCompleted, Is.True);
            Assert.That(swarmMover.IsAtGreenhouse, Is.True);
            Assert.That(mintPatch.GetInteractionText(scenario.Interactor), Is.EqualTo("Harvest Mint"));
            Assert.That(honeyShelf.GetInteractionText(scenario.Interactor), Is.EqualTo("Take Honey Jar"));

            DestroySocketScenario(greenhouseStand);
            Object.DestroyImmediate(honeyShelfRoot);
            Object.DestroyImmediate(mintPatchRoot);
            Object.DestroyImmediate(swarmRoot);
            Object.DestroyImmediate(greenhouseAnchorRoot);
            Object.DestroyImmediate(meadowAnchorRoot);
            DestroyScenario(scenario);
        }

        [Test]
        public void RestartLoop_AfterBeeRewardsUnlocked_ResetsBeePuzzleState()
        {
            TestScenario scenario = CreateScenario();
            SetPrivateField(scenario.Item, "itemId", "lure-flower-pot");
            Assert.That(scenario.Runtime.StartActiveDay(), Is.True);
            Assert.That(scenario.Runtime.TryCompleteTask("bloom-flowers"), Is.True);

            GameObject meadowAnchorRoot = new GameObject("Bee Meadow Anchor");
            meadowAnchorRoot.transform.position = new Vector3(1f, 0.5f, 2f);
            GameObject greenhouseAnchorRoot = new GameObject("Bee Greenhouse Anchor");
            greenhouseAnchorRoot.transform.position = new Vector3(4f, 0.5f, 5f);
            GameObject swarmRoot = new GameObject("Bee Swarm");
            BeeSwarmAnchorMover swarmMover = swarmRoot.AddComponent<BeeSwarmAnchorMover>();
            SetPrivateField(swarmMover, "meadowAnchor", meadowAnchorRoot.transform);
            SetPrivateField(swarmMover, "greenhouseAnchor", greenhouseAnchorRoot.transform);
            InvokePrivateMethod(swarmMover, "Awake");
            InvokePrivateMethod(swarmMover, "OnEnable");
            InvokePrivateMethod(swarmMover, "Start");

            GameObject mintPatchRoot = new GameObject("Mint Patch");
            ItemInteractable mintPatch = mintPatchRoot.AddComponent<ItemInteractable>();
            SetPrivateField(mintPatch, "itemId", "mint-bundle");
            SetPrivateField(mintPatch, "displayName", "Mint Bundle");
            SetPrivateField(mintPatch, "pickupPrompt", "Harvest Mint");
            SetPrivateField(mintPatch, "lockedPickupPrompt", "Pollinate Mint First");
            SetPrivateField(mintPatch, "lockedPickupMessage", "The mint patch is still dormant.");
            SetPrivateField(mintPatch, "requiredCompletedTaskIds", new List<string> { "guide-bees" });
            InvokePrivateMethod(mintPatch, "Awake");
            InvokePrivateMethod(mintPatch, "OnEnable");

            GameObject honeyShelfRoot = new GameObject("Mara Honey Shelf");
            ItemInteractable honeyShelf = honeyShelfRoot.AddComponent<ItemInteractable>();
            SetPrivateField(honeyShelf, "itemId", "honey-jar");
            SetPrivateField(honeyShelf, "displayName", "Honey Jar");
            SetPrivateField(honeyShelf, "pickupPrompt", "Take Honey Jar");
            SetPrivateField(honeyShelf, "lockedPickupPrompt", "Wait For Mara");
            SetPrivateField(honeyShelf, "lockedPickupMessage", "Mara keeps the honey shelf closed until the bees reach the greenhouse.");
            SetPrivateField(honeyShelf, "requiredCompletedTaskIds", new List<string> { "guide-bees" });
            InvokePrivateMethod(honeyShelf, "Awake");
            InvokePrivateMethod(honeyShelf, "OnEnable");

            SocketScenario meadowStand = CreateSocketScenario("Meadow Lure Pot Stand", scenario.Item);
            SocketScenario greenhouseStand = CreateSocketScenario(
                "Greenhouse Lure Pot Stand",
                null,
                new List<string> { "bloom-flowers" },
                swarmMover,
                "guide-bees");
            Assert.That(scenario.Interactor.TryPickUpItem(scenario.Item), Is.True);

            greenhouseStand.Socket.Interact(scenario.Interactor);

            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.True);
            Assert.That(swarmMover.IsAtGreenhouse, Is.True);
            Assert.That(mintPatch.GetInteractionText(scenario.Interactor), Is.EqualTo("Harvest Mint"));
            Assert.That(honeyShelf.GetInteractionText(scenario.Interactor), Is.EqualTo("Take Honey Jar"));

            scenario.Runtime.RestartLoop();

            Assert.That(scenario.Runtime.TryGetTask("guide-bees", out DayLoopTaskSnapshot taskSnapshot), Is.True);
            Assert.That(taskSnapshot.IsCompleted, Is.False);
            Assert.That(swarmMover.IsAtGreenhouse, Is.False);
            Assert.That(swarmRoot.transform.position, Is.EqualTo(meadowAnchorRoot.transform.position));
            Assert.That(mintPatch.GetInteractionText(scenario.Interactor), Is.EqualTo("Pollinate Mint First"));
            Assert.That(honeyShelf.GetInteractionText(scenario.Interactor), Is.EqualTo("Wait For Mara"));
            Assert.That(meadowStand.Socket.HasPlacedItem, Is.True);
            Assert.That(greenhouseStand.Socket.HasPlacedItem, Is.False);
            Assert.That(scenario.Item.IsPlaced, Is.True);
            Assert.That(scenario.Item.transform.parent, Is.SameAs(meadowStand.Anchor));
            Assert.That(scenario.Interactor.HeldItem, Is.Null);

            DestroySocketScenario(greenhouseStand);
            DestroySocketScenario(meadowStand);
            Object.DestroyImmediate(honeyShelfRoot);
            Object.DestroyImmediate(mintPatchRoot);
            Object.DestroyImmediate(swarmRoot);
            Object.DestroyImmediate(greenhouseAnchorRoot);
            Object.DestroyImmediate(meadowAnchorRoot);
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
            SetPrivateField(item, "itemId", "blossom-petals");
            SetPrivateField(item, "displayName", "Blossom Petals");
            SetPrivateField(item, "pickupPrompt", "Collect Blossom Petals");
            SetPrivateField(item, "lockedPickupPrompt", "Bloom Meadow First");
            SetPrivateField(item, "lockedPickupMessage", "The petals are not ready to gather yet.");
            SetPrivateField(item, "requiredCompletedTaskIds", new List<string> { "bloom-flowers" });
            InvokePrivateMethod(item, "Awake");
            InvokePrivateMethod(item, "OnEnable");

            return new TestScenario(runtimeRoot, runtime, playerRoot, interactor, itemRoot, item);
        }

        private static SocketScenario CreateSocketScenario(
            string name,
            ItemInteractable startingItem,
            List<string> requiredCompletedTaskIds = null,
            BeeSwarmAnchorMover beeSwarmMoverOnPlacement = null,
            string taskIdOnPlacement = "")
        {
            GameObject socketRoot = new GameObject(name);
            Transform anchor = CreateAnchor(socketRoot.transform);
            ItemSocketInteractable socket = socketRoot.AddComponent<ItemSocketInteractable>();
            SetPrivateField(socket, "socketAnchor", anchor);
            SetPrivateField(socket, "acceptedItemIds", new List<string> { "lure-flower-pot" });
            SetPrivateField(socket, "placementPrompt", "Place Lure Pot");
            SetPrivateField(socket, "taskIdOnPlacement", taskIdOnPlacement);
            SetPrivateField(socket, "startingItem", startingItem);
            SetPrivateField(socket, "requiredCompletedTaskIds", requiredCompletedTaskIds ?? new List<string>());
            SetPrivateField(socket, "lockedPlacementPrompt", "Bloom Meadow First");
            SetPrivateField(socket, "lockedPlacementMessage", "The lure pot needs the meadow in bloom first.");
            SetPrivateField(socket, "moveBeeSwarmOnPlacement", beeSwarmMoverOnPlacement != null);
            SetPrivateField(socket, "beeSwarmMoverOnPlacement", beeSwarmMoverOnPlacement);
            InvokePrivateMethod(socket, "Awake");
            InvokePrivateMethod(socket, "OnEnable");
            InvokePrivateMethod(socket, "Start");
            return new SocketScenario(socketRoot, socket, anchor);
        }

        private static Transform CreateAnchor(Transform parent)
        {
            GameObject anchor = new GameObject("Anchor");
            anchor.transform.SetParent(parent, false);
            return anchor.transform;
        }

        private static void DestroySocketScenario(SocketScenario scenario)
        {
            Object.DestroyImmediate(scenario.Root);
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
            Object.DestroyImmediate(scenario.ItemRoot);
            Object.DestroyImmediate(scenario.PlayerRoot);
            Object.DestroyImmediate(scenario.RuntimeRoot);
        }

        private readonly struct SocketScenario
        {
            public SocketScenario(GameObject root, ItemSocketInteractable socket, Transform anchor)
            {
                Root = root;
                Socket = socket;
                Anchor = anchor;
            }

            public GameObject Root { get; }
            public ItemSocketInteractable Socket { get; }
            public Transform Anchor { get; }
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
