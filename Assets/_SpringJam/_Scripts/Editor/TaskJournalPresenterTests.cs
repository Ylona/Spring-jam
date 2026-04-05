using NUnit.Framework;
using SpringJam.Systems.DayLoop;
using SpringJam.UI;

namespace SpringJam.Tests.EditMode
{
    public sealed class TaskJournalPresenterTests
    {
        [Test]
        public void GetTaskState_MapsLockedReadyAndComplete()
        {
            Assert.That(
                TaskJournalPresenter.GetTaskState(new DayLoopTaskSnapshot("cook-spring-meal", "Cook", false, false, false)),
                Is.EqualTo(TaskJournalTaskState.Locked));
            Assert.That(
                TaskJournalPresenter.GetTaskState(new DayLoopTaskSnapshot("guide-bees", "Bees", true, false, true)),
                Is.EqualTo(TaskJournalTaskState.Ready));
            Assert.That(
                TaskJournalPresenter.GetTaskState(new DayLoopTaskSnapshot("learn-routines", "Routines", true, true, true)),
                Is.EqualTo(TaskJournalTaskState.Complete));
        }

        [Test]
        public void GetTaskStateText_UsesTaskSpecificCopyForCookingLock()
        {
            string lockedText = TaskJournalPresenter.GetTaskStateText(
                new DayLoopTaskSnapshot("cook-spring-meal", "Cook", false, false, false));
            string readyText = TaskJournalPresenter.GetTaskStateText(
                new DayLoopTaskSnapshot("cook-spring-meal", "Cook", true, false, false));

            Assert.That(lockedText, Is.EqualTo("Awaiting the valley"));
            Assert.That(readyText, Is.EqualTo("Ready for the table"));
        }

        [Test]
        public void GetTimeBand_UsesSymbolicDayStagesInsteadOfNumbers()
        {
            Assert.That(TaskJournalPresenter.GetTimeBand(DayLoopPhase.StartDay, 0f, 120f), Is.EqualTo(TaskJournalTimeBand.Dawn));
            Assert.That(TaskJournalPresenter.GetTimeBand(DayLoopPhase.ActiveDay, 10f, 120f), Is.EqualTo(TaskJournalTimeBand.Morning));
            Assert.That(TaskJournalPresenter.GetTimeBand(DayLoopPhase.ActiveDay, 42f, 120f), Is.EqualTo(TaskJournalTimeBand.HighSun));
            Assert.That(TaskJournalPresenter.GetTimeBand(DayLoopPhase.ActiveDay, 78f, 120f), Is.EqualTo(TaskJournalTimeBand.LongLight));
            Assert.That(TaskJournalPresenter.GetTimeBand(DayLoopPhase.ActiveDay, 108f, 120f), Is.EqualTo(TaskJournalTimeBand.Dusk));
        }

        [Test]
        public void GetClosedPetalCount_ClosesMorePetalsAsDuskApproaches()
        {
            Assert.That(TaskJournalPresenter.GetClosedPetalCount(DayLoopPhase.StartDay, 0f, 120f, 5), Is.EqualTo(0));
            Assert.That(TaskJournalPresenter.GetClosedPetalCount(DayLoopPhase.ActiveDay, 24f, 120f, 5), Is.EqualTo(1));
            Assert.That(TaskJournalPresenter.GetClosedPetalCount(DayLoopPhase.ActiveDay, 66f, 120f, 5), Is.EqualTo(3));
            Assert.That(TaskJournalPresenter.GetClosedPetalCount(DayLoopPhase.ActiveDay, 114f, 120f, 5), Is.EqualTo(5));
        }
    }
}
