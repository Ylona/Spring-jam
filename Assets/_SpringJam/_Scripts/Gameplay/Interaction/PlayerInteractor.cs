using UnityEngine;
using SpringJam.Systems.DayLoop;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform interactPoint;

    private InputSystem_Actions controls;
    private IInteractable currentInteractable;

    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        FindInteractable();

        if (controls.Player.Interact.WasPressedThisFrame() && currentInteractable != null && CanInteract())
        {
            currentInteractable.Interact();
        }
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

    private static bool CanInteract()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        return runtime == null || runtime.CurrentPhase == DayLoopPhase.ActiveDay;
    }

}