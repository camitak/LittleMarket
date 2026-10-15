using System.Collections;
using UnityEngine;

public class RecyclingBin :
    MonoBehaviour,
    IInteractable
{
    [Header("Feedback")]
    [SerializeField]
    private Transform feedbackVisual;

    [Min(0f)]
    [SerializeField]
    private float feedbackLift = 0.08f;

    [Min(0.05f)]
    [SerializeField]
    private float feedbackDuration = 0.22f;

    private Vector3 feedbackBaseLocalPosition;

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (feedbackVisual != null)
        {
            feedbackBaseLocalPosition =
                feedbackVisual.localPosition;
        }
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        DeliveryBox heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox != null)
        {
            if (!heldBox.IsEmpty)
            {
                return "Carton still contains "
                       + heldBox.RemainingQuantity
                       + " product"
                       + (
                           heldBox.RemainingQuantity == 1
                               ? ""
                               : "s"
                       );
            }

            return "[E] Recycle empty carton";
        }

        if (player.GetHeldRestockBasket()
            != null)
        {
            return "This bin is for empty cartons";
        }

        if (player.GetHeldItem()
            != null)
        {
            return "This bin is for empty cartons";
        }

        return "Cardboard recycling"
               + "\nBring empty cartons here";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        DeliveryBox heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox == null ||
            !heldBox.IsEmpty)
        {
            return;
        }

        /*
         * Remove the player's reference first.
         * Then destroy the actual carton.
         *
         * If this carton belongs to a DeliveryZone
         * slot, Unity's destroyed-object null
         * behaviour also makes that slot reusable.
         */
        player.RemoveHeldDeliveryBox();

        Destroy(
            heldBox.gameObject
        );

        PlayRecycleFeedback();
    }

    private void PlayRecycleFeedback()
    {
        if (feedbackVisual == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(
                feedbackRoutine
            );
        }

        feedbackVisual.localPosition =
            feedbackBaseLocalPosition;

        feedbackRoutine =
            StartCoroutine(
                RecycleFeedbackRoutine()
            );
    }

    private IEnumerator
        RecycleFeedbackRoutine()
    {
        float duration =
            Mathf.Max(
                0.05f,
                feedbackDuration
            );

        float elapsed =
            0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float lift =
                Mathf.Sin(
                    t * Mathf.PI
                )
                * feedbackLift;

            feedbackVisual.localPosition =
                feedbackBaseLocalPosition
                + Vector3.up
                * lift;

            yield return null;
        }

        feedbackVisual.localPosition =
            feedbackBaseLocalPosition;

        feedbackRoutine =
            null;
    }
}