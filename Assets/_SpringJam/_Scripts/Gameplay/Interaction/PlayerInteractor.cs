using UnityEngine;
using SpringJam.Systems.DayLoop;

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
        input.OnInteract += TryInteract;

    }

    private void OnDestroy()
    {
        input.OnInteract -= TryInteract;
    }

    private void Update()
    {
        FindInteractable();

    }

    private void TryInteract()
    {
        currentInteractable?.Interact();
    }

    private void FindInteractable()
    {
        currentInteractable = null;

        Collider[] hits = Physics.OverlapSphere(interactPoint.position, interactRange, interactableLayer);

        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float distance = Vector3.Distance(interactPoint.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentInteractable = interactable;
            }
        }
    }

    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }

    private void OnDrawGizmosSelected()
    {
        if (interactPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactPoint.position, interactRange);
    }


}