using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpringJam.Systems.DayLoop;
using UnityEngine;

namespace SpringJam.Tests.EditMode
{
    public sealed class FlowerBloomPuzzleControllerTests
    {
        [Test]
        public void TryActivateBed_ProgressedBedEntersActivatedState()
        {
            TestScenario scenario = CreateScenario();

            FlowerBedActivationResult result = scenario.Controller.TryActivateBed(scenario.Beds[0]);

            Assert.That(result, Is.EqualTo(FlowerBedActivationResult.Progressed));
            Assert.That(scenario.Controller.CurrentFeedbackState, Is.EqualTo(FlowerBloomPuzzleFeedbackState.InProgress));
            Assert.That(scenario.Beds[0].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Activated));
            Assert.That(scenario.Beds[1].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Dormant));

            DestroyScenario(scenario);
        }

        [Test]
        public void TryActivateBed_RejectedBedsStayFailedUntilNextAttempt()
        {
            TestScenario scenario = CreateScenario();

            scenario.Controller.TryActivateBed(scenario.Beds[0]);
            FlowerBedActivationResult failureResult = scenario.Controller.TryActivateBed(scenario.Beds[2]);

            Assert.That(failureResult, Is.EqualTo(FlowerBedActivationResult.Rejected));
            Assert.That(scenario.Controller.CurrentFeedbackState, Is.EqualTo(FlowerBloomPuzzleFeedbackState.Failed));
            Assert.That(scenario.Controller.IsShowingFailureFeedback, Is.True);
            Assert.That(scenario.Beds[0].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Failed));
            Assert.That(scenario.Beds[1].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Failed));
            Assert.That(scenario.Beds[2].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Failed));

            FlowerBedActivationResult retryResult = scenario.Controller.TryActivateBed(scenario.Beds[0]);

            Assert.That(retryResult, Is.EqualTo(FlowerBedActivationResult.Progressed));
            Assert.That(scenario.Controller.CurrentFeedbackState, Is.EqualTo(FlowerBloomPuzzleFeedbackState.InProgress));
            Assert.That(scenario.Controller.IsShowingFailureFeedback, Is.False);
            Assert.That(scenario.Beds[0].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Activated));
            Assert.That(scenario.Beds[1].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Dormant));
            Assert.That(scenario.Beds[2].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Dormant));

            DestroyScenario(scenario);
        }

        [Test]
        public void TryActivateBed_CompletionMarksAllBedsCompleted()
        {
            TestScenario scenario = CreateScenario();

            scenario.Controller.TryActivateBed(scenario.Beds[0]);
            scenario.Controller.TryActivateBed(scenario.Beds[1]);
            FlowerBedActivationResult result = scenario.Controller.TryActivateBed(scenario.Beds[2]);

            Assert.That(result, Is.EqualTo(FlowerBedActivationResult.Completed));
            Assert.That(scenario.Controller.CurrentFeedbackState, Is.EqualTo(FlowerBloomPuzzleFeedbackState.Completed));
            Assert.That(scenario.Beds[0].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Completed));
            Assert.That(scenario.Beds[1].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Completed));
            Assert.That(scenario.Beds[2].CurrentFeedbackState, Is.EqualTo(FlowerBedFeedbackState.Completed));

            DestroyScenario(scenario);
        }

        [Test]
        public void TryActivateBed_CompletionCompletesBloomFlowersTaskWhenRuntimeIsPlayable()
        {
            GameObject runtimeRoot = new GameObject("DayLoopRuntime");
            DayLoopRuntime runtime = runtimeRoot.AddComponent<DayLoopRuntime>();
            InitializeRuntime(runtime);
            Assert.That(runtime.StartActiveDay(), Is.True);

            DayLoopTaskSnapshot changedTask = null;
            runtime.TaskChanged += snapshot =>
            {
                if (snapshot.TaskId == "bloom-flowers")
                {
                    changedTask = snapshot;
                }
            };

            TestScenario scenario = CreateScenario();

            scenario.Controller.TryActivateBed(scenario.Beds[0]);
            scenario.Controller.TryActivateBed(scenario.Beds[1]);
            FlowerBedActivationResult result = scenario.Controller.TryActivateBed(scenario.Beds[2]);

            Assert.That(result, Is.EqualTo(FlowerBedActivationResult.Completed));
            Assert.That(runtime.TryGetTask("bloom-flowers", out DayLoopTaskSnapshot taskSnapshot), Is.True);
            Assert.That(taskSnapshot.IsCompleted, Is.True);
            Assert.That(changedTask, Is.Not.Null);
            Assert.That(changedTask.IsCompleted, Is.True);

            DestroyScenario(scenario);
            Object.DestroyImmediate(runtimeRoot);
        }

        private static TestScenario CreateScenario()
        {
            GameObject root = new GameObject("FlowerPuzzle");
            FlowerBloomPuzzleController controller = root.AddComponent<FlowerBloomPuzzleController>();
            List<FlowerBedInteractable> beds = new List<FlowerBedInteractable>
            {
                CreateBed(root.transform, "snowdrop", "Snowdrop"),
                CreateBed(root.transform, "crocus", "Crocus"),
                CreateBed(root.transform, "tulip", "Tulip"),
            };

            SetPrivateField(controller, "orderedFlowerBeds", beds);
            return new TestScenario(root, controller, beds);
        }

        private static FlowerBedInteractable CreateBed(Transform parent, string flowerId, string displayName)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = displayName + " Bed";
            gameObject.transform.SetParent(parent, false);

            FlowerBedInteractable bed = gameObject.AddComponent<FlowerBedInteractable>();
            SetPrivateField(bed, "flowerId", flowerId);
            SetPrivateField(bed, "displayName", displayName);
            return bed;
        }

        private static void InitializeRuntime(DayLoopRuntime runtime)
        {
            if (DayLoopRuntime.Instance != runtime)
            {
                InvokePrivateMethod(runtime, "Awake");
            }

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
            Object.DestroyImmediate(scenario.Root);
        }

        private readonly struct TestScenario
        {
            public TestScenario(GameObject root, FlowerBloomPuzzleController controller, List<FlowerBedInteractable> beds)
            {
                Root = root;
                Controller = controller;
                Beds = beds;
            }

            public GameObject Root { get; }
            public FlowerBloomPuzzleController Controller { get; }
            public List<FlowerBedInteractable> Beds { get; }
        }
    }
}
