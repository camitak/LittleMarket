using UnityEngine;

public class OrderingTerminal : MonoBehaviour, IInteractable
{
    [SerializeField] private OrderingUI orderingUI;

    public string GetInteractionPrompt(PlayerInteraction player)
    {
        if (player.GetHeldItem() != null)
        {
            return "Put down item to use terminal";
        }

        return "[E] Use ordering terminal";
    }

    public void Interact(PlayerInteraction player)
    {
        if (player.GetHeldItem() != null)
        {
            return;
        }

        orderingUI.Open();
    }
}