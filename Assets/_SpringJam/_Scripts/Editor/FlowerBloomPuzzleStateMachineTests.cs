using System;
using NUnit.Framework;

namespace SpringJam.Tests.EditMode
{
    public sealed class FlowerBloomPuzzleStateMachineTests
    {
        [Test]
        public void TryActivate_CorrectOrder_ProgressesToCompletion()
        {
            FlowerBloomPuzzleStateMachine machine = new FlowerBloomPuzzleStateMachine(
                new[] { "snowdrop", "crocus", "tulip" });

            Assert.That(machine.ExpectedFlowerId, Is.EqualTo("snowdrop"));
            Assert.That(machine.TryActivate("snowdrop"), Is.EqualTo(FlowerBedActivationResult.Progressed));
            Assert.That(machine.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(machine.ExpectedFlowerId, Is.EqualTo("crocus"));
            Assert.That(machine.TryActivate("crocus"), Is.EqualTo(FlowerBedActivationResult.Progressed));
            Assert.That(machine.TryActivate("tulip"), Is.EqualTo(FlowerBedActivationResult.Completed));
            Assert.That(machine.IsCompleted, Is.True);
            Assert.That(machine.CurrentStepIndex, Is.EqualTo(3));
            Assert.That(machine.ExpectedFlowerId, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TryActivate_WrongFlower_ResetsProgress()
        {
            FlowerBloomPuzzleStateMachine machine = new FlowerBloomPuzzleStateMachine(
                new[] { "snowdrop", "crocus", "tulip" });

            machine.TryActivate("snowdrop");

            Assert.That(machine.TryActivate("tulip"), Is.EqualTo(FlowerBedActivationResult.Rejected));
            Assert.That(machine.IsCompleted, Is.False);
            Assert.That(machine.CurrentStepIndex, Is.EqualTo(0));
            Assert.That(machine.ExpectedFlowerId, Is.EqualTo("snowdrop"));
        }

        [Test]
        public void TryActivate_AfterCompletion_IsIgnored()
        {
            FlowerBloomPuzzleStateMachine machine = new FlowerBloomPuzzleStateMachine(
                new[] { "snowdrop" });

            Assert.That(machine.TryActivate("snowdrop"), Is.EqualTo(FlowerBedActivationResult.Completed));
            Assert.That(machine.TryActivate("snowdrop"), Is.EqualTo(FlowerBedActivationResult.Ignored));
        }

        [Test]
        public void Constructor_DuplicateIds_Throws()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                new FlowerBloomPuzzleStateMachine(new[] { "snowdrop", "Snowdrop" }));

            Assert.That(exception.Message, Does.Contain("Duplicate flower bed id"));
        }
    }
}

