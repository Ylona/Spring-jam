public interface IInteractable
{
    void Interact(PlayerInteractor interactor);
    string GetInteractionText(PlayerInteractor interactor);
}