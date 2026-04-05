using UnityEngine;

namespace SpringJam.UI
{
    [DisallowMultipleComponent]
    public sealed class TaskJournalRuntimeController : MonoBehaviour
    {
        private void Awake()
        {
            // The task journal is rendered by DialogueRuntimeController now.
            enabled = false;
        }
    }
}
