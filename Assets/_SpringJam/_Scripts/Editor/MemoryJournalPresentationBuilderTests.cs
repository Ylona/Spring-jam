using System.Linq;
using NUnit.Framework;
using SpringJam.Journal;
using SpringJam.Systems.DayLoop;

namespace SpringJam.Tests.EditMode
{
    public sealed class MemoryJournalPresentationBuilderTests
    {
        [Test]
        public void Build_UsesReadableCopyForKnownTasksAndClues()
        {
            MemoryJournalPageData page = MemoryJournalPresentationBuilder.Build(CreateSnapshot());

            MemoryJournalTaskEntry cookingTask = page.Tasks.Single(task => task.TaskId == "cook-spring-meal");
            MemoryJournalClueEntry clue = page.Clues.Single(entry => entry.KnowledgeId == "bunny-loop-hint");

            Assert.That(cookingTask.State, Is.EqualTo(MemoryJournalTaskState.Sleeping));
            Assert.That(cookingTask.StatusLabel, Is.EqualTo("Sleeping"));
            Assert.That(cookingTask.Summary, Is.EqualTo("This page stays folded shut until the rest of the journal settles."));
            Assert.That(clue.CategoryLabel, Is.EqualTo("Bunny"));
            Assert.That(clue.Title, Is.EqualTo("Seeds Near the Old Path"));
        }

        [Test]
        public void Build_HumanizesUnknownKnowledgeIds()
        {
            DayLoopSnapshot snapshot = new DayLoopSnapshot(
                1,
                DayLoopPhase.ActiveDay,
                10f,
                120f,
                10f,
                120f,
                new[]
                {
                    new DayLoopTaskSnapshot("bloom-flowers", "Help the flowers bloom", true, false, true),
                },
                new[] { "greenhouse-open" });

            MemoryJournalPageData page = MemoryJournalPresentationBuilder.Build(snapshot);
            MemoryJournalClueEntry clue = page.Clues.Single();

            Assert.That(clue.Title, Is.EqualTo("Greenhouse Open"));
            Assert.That(clue.Summary, Is.EqualTo("You found a detail worth carrying into the next dawn."));
            Assert.That(clue.CategoryLabel, Is.EqualTo("Memory"));
        }

        [Test]
        public void Build_ReportsQuietStateWithoutRuntimeSnapshot()
        {
            MemoryJournalPageData page = MemoryJournalPresentationBuilder.Build(null);

            Assert.That(page.PhaseLine, Is.EqualTo("The pages are quiet until the valley wakes."));
            Assert.That(page.Tasks, Is.Empty);
            Assert.That(page.Clues, Is.Empty);
            Assert.That(page.CluesEmptyMessage, Is.EqualTo("Empty pages wait for the valley to tell you something worth keeping."));
        }

        private static DayLoopSnapshot CreateSnapshot()
        {
            return new DayLoopSnapshot(
                1,
                DayLoopPhase.ActiveDay,
                25f,
                120f,
                25f,
                120f,
                new[]
                {
                    new DayLoopTaskSnapshot("bloom-flowers", "Help the flowers bloom", true, true, true),
                    new DayLoopTaskSnapshot("guide-bees", "Guide the bees to the right plants", true, false, true),
                    new DayLoopTaskSnapshot("learn-routines", "Learn the villagers' routines", true, false, true),
                    new DayLoopTaskSnapshot("cook-spring-meal", "Cook a spring meal", false, false, false),
                },
                new[] { "bunny-loop-hint" });
        }
    }
}
