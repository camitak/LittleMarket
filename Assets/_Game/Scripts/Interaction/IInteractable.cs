public interface IInteractable
{
    string GetInteractionPrompt();

    void Interact(PlayerInteraction player);
}