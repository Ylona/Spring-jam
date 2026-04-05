using SpringJam.Dialogue;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform interactPoint;

    private PlayerInputHandler input;
    private IInteractable currentInteractable;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        if (input != null)
        {
            input.OnInteract += TryInteract;
        }
    }

    private void OnDisable()
    {
        DialogueRuntimeController.SetInteractionPrompt(string.Empty);
    }

    private void OnDestroy()
    {
        if (input != null)
        {
            input.OnInteract -= TryInteract;
        }
    }

    private void Update()
    {
        FindInteractable();
        UpdatePrompt();
    }

    private void TryInteract()
    {
        if (!CanInteract() || DialogueRuntimeController.ConsumedInputThisFrame)
        {
            return;
        }

        currentInteractable?.Interact();
    }

    private void FindInteractable()
    {
        currentInteractable = null;
        if (interactPoint == null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(interactPoint.position, interactRange, interactableLayer);
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            float distance = Vector3.Distance(interactPoint.position, hit.transform.position);
            if (distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            currentInteractable = interactable;
        }
    }

    private void UpdatePrompt()
    {
        string promptText = CanInteract() && currentInteractable != null
            ? currentInteractable.GetInteractionText()
            : string.Empty;
        DialogueRuntimeController.SetInteractionPrompt(promptText);
    }

    private static bool CanInteract()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        bool dayAllowsInteraction = runtime == null || runtime.CurrentPhase == DayLoopPhase.ActiveDay;
        return dayAllowsInteraction && !DialogueRuntimeController.IsDialogueOpen;
    }

    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }

    private void OnDrawGizmosSelected()
    {
        if (interactPoint == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactPoint.position, interactRange);
    }
}
