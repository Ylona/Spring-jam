using System.Linq;
using NUnit.Framework;
using SpringJam.Systems.DayLoop;

namespace SpringJam.Tests.EditMode
{
    public sealed class DayLoopStateMachineTests
    {
        [Test]
        public void Begin_StartsInStartDayState()
        {
            DayLoopStateMachine machine = CreateMachine();

            machine.Begin();
            DayLoopSnapshot snapshot = machine.CurrentSnapshot;

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.LoopIndex, Is.EqualTo(1));
            Assert.That(snapshot.Phase, Is.EqualTo(DayLoopPhase.StartDay));
            Assert.That(snapshot.PhaseDurationSeconds, Is.EqualTo(3f));
            Assert.That(snapshot.ElapsedSeconds, Is.EqualTo(0f));
            Assert.That(GetTask(snapshot, "bloom-flowers").IsUnlocked, Is.True);
            Assert.That(GetTask(snapshot, "cook-spring-meal").IsUnlocked, Is.False);
        }

        [Test]
        public void StartDay_BlocksGameplayActionsUntilActiveDayBegins()
        {
            DayLoopStateMachine machine = CreateMachine();
            machine.Begin();

            Assert.That(machine.TryCompleteTask("bloom-flowers"), Is.False);
            Assert.That(machine.TryLearnKnowledge("bee-route"), Is.False);

            bool started = machine.StartActiveDay();

            Assert.That(started, Is.True);
            Assert.That(machine.CurrentSnapshot.Phase, Is.EqualTo(DayLoopPhase.ActiveDay));
        }

        [Test]
        public void Tick_AutoTransitionsOutOfStartDayAndCarriesRemainingTime()
        {
            DayLoopStateMachine machine = CreateMachine();
            DayLoopSnapshot phaseChangeSnapshot = null;
            machine.PhaseChanged += snapshot => phaseChangeSnapshot = snapshot;
            machine.Begin();

            machine.Tick(4f);

            Assert.That(phaseChangeSnapshot, Is.Not.Null);
            Assert.That(phaseChangeSnapshot.Phase, Is.EqualTo(DayLoopPhase.ActiveDay));
            Assert.That(machine.CurrentSnapshot.Phase, Is.EqualTo(DayLoopPhase.ActiveDay));
            Assert.That(machine.CurrentSnapshot.ElapsedSeconds, Is.EqualTo(1f));
        }

        [Test]
        public void CompletingPrerequisites_UnlocksCookingTask()
        {
            DayLoopStateMachine machine = CreateMachine();
            machine.Begin();
            machine.StartActiveDay();

            machine.TryCompleteTask("bloom-flowers");
            machine.TryCompleteTask("guide-bees");
            machine.TryCompleteTask("learn-routines");

            DayLoopTaskSnapshot cookingTask = GetTask(machine.CurrentSnapshot, "cook-spring-meal");
            Assert.That(cookingTask.IsUnlocked, Is.True);
            Assert.That(cookingTask.IsCompleted, Is.False);
        }

        [Test]
        public void Knowledge_PersistsAcrossTimedLoopReset()
        {
            DayLoopStateMachine machine = CreateMachine();
            DayLoopEndContext endingContext = null;
            machine.LoopEnded += context => endingContext = context;
            machine.Begin();
            machine.StartActiveDay();

            machine.TryLearnKnowledge("bee-route");
            machine.Tick(120f);

            Assert.That(endingContext, Is.Not.Null);
            Assert.That(endingContext.Reason, Is.EqualTo(DayLoopEndReason.TimeExpired));
            Assert.That(endingContext.EndingSnapshot.LearnedKnowledge, Does.Contain("bee-route"));
            Assert.That(machine.CurrentSnapshot.LoopIndex, Is.EqualTo(2));
            Assert.That(machine.CurrentSnapshot.Phase, Is.EqualTo(DayLoopPhase.StartDay));
            Assert.That(machine.CurrentSnapshot.LearnedKnowledge, Does.Contain("bee-route"));
            Assert.That(machine.CurrentSnapshot.Tasks.All(task => !task.IsCompleted), Is.True);
        }

        [Test]
        public void CompletingAllTasks_StartsFreshLoopBackInStartDayState()
        {
            DayLoopStateMachine machine = CreateMachine();
            DayLoopEndContext endingContext = null;
            machine.LoopEnded += context => endingContext = context;
            machine.Begin();
            machine.StartActiveDay();

            machine.TryCompleteTask("bloom-flowers");
            machine.TryCompleteTask("guide-bees");
            machine.TryCompleteTask("learn-routines");
            machine.TryCompleteTask("cook-spring-meal");

            Assert.That(endingContext, Is.Not.Null);
            Assert.That(endingContext.Reason, Is.EqualTo(DayLoopEndReason.SuccessfulLoop));
            Assert.That(machine.CurrentSnapshot.LoopIndex, Is.EqualTo(2));
            Assert.That(machine.CurrentSnapshot.Phase, Is.EqualTo(DayLoopPhase.StartDay));
            Assert.That(GetTask(machine.CurrentSnapshot, "cook-spring-meal").IsUnlocked, Is.False);
            Assert.That(machine.CurrentSnapshot.Tasks.All(task => !task.IsCompleted), Is.True);
        }

        private static DayLoopStateMachine CreateMachine()
        {
            return new DayLoopStateMachine(
                120f,
                3f,
                new[]
                {
                    new DayLoopTaskDefinition("bloom-flowers", "Help the flowers bloom", true, true),
                    new DayLoopTaskDefinition("guide-bees", "Guide the bees", true, true),
                    new DayLoopTaskDefinition("learn-routines", "Learn the routines", true, true),
                    new DayLoopTaskDefinition("cook-spring-meal", "Cook a spring meal", false, false),
                });
        }

        private static DayLoopTaskSnapshot GetTask(DayLoopSnapshot snapshot, string taskId)
        {
            return snapshot.Tasks.Single(task => task.TaskId == taskId);
        }
    }
}
