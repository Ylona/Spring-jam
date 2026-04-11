using System.Collections.Generic;
using NUnit.Framework;
using SpringJam.Dialogue;

namespace SpringJam.Tests.EditMode
{
    public sealed class DialogueSessionTests
    {
        [Test]
        public void Session_AdvancesRewindsAndCompletesConversation()
        {
            bool completed = false;
            DialogueConversation conversation = new DialogueConversation(
                "villager-hint",
                "Talk",
                new[]
                {
                    new DialogueLine("Mara", "The bees always like the sunny bed first."),
                    new DialogueLine("Mara", "Watch where they hover when the dew clears."),
                },
                () => completed = true);

            DialogueSession session = new DialogueSession();

            Assert.That(session.TryOpen(conversation), Is.True);
            Assert.That(session.CurrentLineIndex, Is.EqualTo(0));
            Assert.That(session.CurrentLine.Body, Is.EqualTo("The bees always like the sunny bed first."));
            Assert.That(session.MovePrevious(), Is.False);

            Assert.That(session.Advance(), Is.EqualTo(DialogueAdvanceResult.Advanced));
            Assert.That(session.CurrentLineIndex, Is.EqualTo(1));
            Assert.That(session.CanMovePrevious, Is.True);

            Assert.That(session.MovePrevious(), Is.True);
            Assert.That(session.CurrentLineIndex, Is.EqualTo(0));

            Assert.That(session.Advance(), Is.EqualTo(DialogueAdvanceResult.Advanced));
            Assert.That(session.Advance(), Is.EqualTo(DialogueAdvanceResult.Completed));
            Assert.That(session.IsOpen, Is.False);
            Assert.That(completed, Is.True);
        }

        [Test]
        public void Session_CloseEarly_DoesNotCompleteConversation()
        {
            bool completed = false;
            DialogueConversation conversation = new DialogueConversation(
                "short-bark",
                "Talk",
                new[]
                {
                    new DialogueLine("Pip", "Not yet."),
                },
                () => completed = true);

            DialogueSession session = new DialogueSession();
            session.TryOpen(conversation);

            Assert.That(session.TryClose(), Is.True);
            Assert.That(session.IsOpen, Is.False);
            Assert.That(completed, Is.False);
        }

        [Test]
        public void DialogueConditions_CheckKnowledgeAndTaskState()
        {
            DialogueConditionDefinition conditions = new DialogueConditionDefinition(
                new[] { "rabbit-seeds" },
                new[] { "greenhouse-open" },
                new[] { "learn-routines" },
                new[] { "cook-spring-meal" });

            DialogueProgressSnapshot matchingSnapshot = new DialogueProgressSnapshot(
                new[] { "rabbit-seeds" },
                new Dictionary<string, bool>
                {
                    { "learn-routines", true },
                    { "cook-spring-meal", false },
                });

            DialogueProgressSnapshot blockedSnapshot = new DialogueProgressSnapshot(
                new[] { "rabbit-seeds", "greenhouse-open" },
                new Dictionary<string, bool>
                {
                    { "learn-routines", true },
                    { "cook-spring-meal", false },
                });

            Assert.That(conditions.Matches(matchingSnapshot), Is.True);
            Assert.That(conditions.Matches(blockedSnapshot), Is.False);
            Assert.That(conditions.Matches(DialogueProgressSnapshot.Empty), Is.False);
        }
    }
}
