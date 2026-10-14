using UnityEngine;

public class OrderingTerminal :
    MonoBehaviour,
    IInteractable
{
    [SerializeField]
    private OrderingUI orderingUI;

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        if (player.HasAnythingHeld)
        {
            return "Put down what you're carrying "
                   + "to use the terminal";
        }

        return "[E] Use ordering terminal";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.HasAnythingHeld)
        {
            return;
        }

        if (orderingUI == null)
        {
            return;
        }

        orderingUI.Open();
    }
}