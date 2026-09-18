public interface IInteractable
{
    string GetInteractionPrompt(PlayerInteraction player);

    void Interact(PlayerInteraction player);
}