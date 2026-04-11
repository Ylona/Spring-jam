namespace SpringJam.Dialogue
{
    public enum DialogueAdvanceResult
    {
        None,
        Advanced,
        Completed,
    }

    public sealed class DialogueSession
    {
        private DialogueConversation activeConversation;
        private int currentLineIndex = -1;

        public bool IsOpen => activeConversation != null;
        public DialogueConversation ActiveConversation => activeConversation;
        public int CurrentLineIndex => currentLineIndex;
        public DialogueLine CurrentLine => IsOpen ? activeConversation.Lines[currentLineIndex] : null;
        public bool CanMovePrevious => IsOpen && currentLineIndex > 0;
        public bool IsOnLastLine => IsOpen && currentLineIndex >= activeConversation.LineCount - 1;

        public bool TryOpen(DialogueConversation conversation)
        {
            if (IsOpen || conversation == null || conversation.LineCount == 0)
            {
                return false;
            }

            activeConversation = conversation;
            currentLineIndex = 0;
            return true;
        }

        public DialogueAdvanceResult Advance()
        {
            if (!IsOpen)
            {
                return DialogueAdvanceResult.None;
            }

            if (!IsOnLastLine)
            {
                currentLineIndex++;
                return DialogueAdvanceResult.Advanced;
            }

            DialogueConversation completedConversation = activeConversation;
            CloseInternal();
            completedConversation.Complete();
            return DialogueAdvanceResult.Completed;
        }

        public bool MovePrevious()
        {
            if (!CanMovePrevious)
            {
                return false;
            }

            currentLineIndex--;
            return true;
        }

        public bool TryClose()
        {
            if (!IsOpen)
            {
                return false;
            }

            CloseInternal();
            return true;
        }

        private void CloseInternal()
        {
            activeConversation = null;
            currentLineIndex = -1;
        }
    }
}
