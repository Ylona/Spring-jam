using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.Events;

public sealed class BasketInteractable : ItemInteractable
{
    [Header("Routine Basket")]
    [SerializeField] private string taskId = "learn-routines";
    [SerializeField] private bool completeTaskOnPickup = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onBasketPickedUp;

    private BasketController controller;

    public void SetController(BasketController value)
    {
        controller = value;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        base.Interact(interactor);

        if (completeTaskOnPickup)
        {
            DayLoopRuntime.Instance?.TryCompleteTask(taskId);
            controller?.NotifyBasketCollected();
        }

        onBasketPickedUp?.Invoke();
    }
}