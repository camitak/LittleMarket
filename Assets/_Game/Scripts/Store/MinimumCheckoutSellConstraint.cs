using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CheckoutRegister))]
public class MinimumCheckoutSellConstraint : MonoBehaviour, IFurnitureSellConstraint
{
    public bool CanSellFurniture(out string reason)
    {
        CheckoutRegister currentCheckout = GetComponent<CheckoutRegister>();

        CheckoutRegister[] checkouts = FindObjectsByType<CheckoutRegister>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (CheckoutRegister checkout in checkouts)
        {
            if (checkout == null ||
                checkout == currentCheckout ||
                !checkout.isActiveAndEnabled)
            {
                continue;
            }

            FurnitureIdentity identity = checkout.GetComponent<FurnitureIdentity>();

            if (identity != null && identity.isActiveAndEnabled)
            {
                reason = "";
                return true;
            }
        }

        reason = "At least one checkout counter is required to serve customers.";
        return false;
    }
}