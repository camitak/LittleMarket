using UnityEngine;

public class OrderingTerminal :
    MonoBehaviour,
    IInteractable,
    IManagementInteractable
{
    [Header("UI")]
    [SerializeField] private OrderingUI orderingUI;

    [Header("Store")]
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private CustomerFlow customerFlow;

    public float ManagementHoldDuration => 0.8f;

    public string GetInteractionPrompt(PlayerInteraction player)
    {
        if (player == null)
        {
            return "";
        }

        if (player.HasAnythingHeld)
        {
            return "Put down what you're carrying to use the terminal";
        }

        if (storeClock == null)
        {
            return "[E] Use ordering terminal";
        }

        if (!storeClock.IsPastClosingTime)
        {
            return "[E] Use ordering terminal";
        }

        int customersRemaining =
            customerFlow != null
                ? customerFlow.ActiveCustomerCount
                : 0;

        if (customersRemaining > 0)
        {
            string customerWord =
                customersRemaining == 1
                    ? "customer"
                    : "customers";

            return
                "[E] Use ordering terminal\n" +
                $"Store closed • {customersRemaining} {customerWord} still inside";
        }

        return
            "[E] Use ordering terminal\n" +
            "[Hold X] End day";
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null || player.HasAnythingHeld)
        {
            return;
        }

        if (orderingUI == null)
        {
            return;
        }

        orderingUI.Open();
    }

    public bool CanUseManagementAction(PlayerInteraction player)
    {
        if (player == null ||
            player.HasAnythingHeld ||
            storeClock == null ||
            storeClock.HasDayEnded ||
            !storeClock.IsPastClosingTime)
        {
            return false;
        }

        if (customerFlow != null &&
            customerFlow.ActiveCustomerCount > 0)
        {
            return false;
        }

        return true;
    }

    public void ManagementInteract(PlayerInteraction player)
    {
        if (!CanUseManagementAction(player))
        {
            return;
        }

        storeClock.EndDayNow();
    }
}