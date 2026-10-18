public interface IInteractable
{
    string GetInteractionPrompt(
        PlayerInteraction player
    );

    /*
     * E
     *
     * Natural primary action for this target.
     */
    void Interact(
        PlayerInteraction player
    );
}

/*
 * R
 *
 * Optional secondary / reverse action.
 */
public interface ISecondaryInteractable
{
    void SecondaryInteract(
        PlayerInteraction player
    );
}

/*
 * F
 *
 * Optional whole-object / bulk carry action.
 */
public interface ICarryInteractable
{
    void CarryInteract(
        PlayerInteraction player
    );
}

/*
 * X
 *
 * Optional management action.
 *
 * These are deliberately hold-to-confirm actions
 * because they may change organisation/state.
 */
public interface IManagementInteractable
{
    float ManagementHoldDuration { get; }

    bool CanUseManagementAction(
        PlayerInteraction player
    );

    void ManagementInteract(
        PlayerInteraction player
    );
}

/*
 * Allows an interactable to decide for itself
 * whether holding E or R should repeat.
 *
 * PlayerInteraction no longer needs to know what
 * concrete type of object it is looking at.
 */
public interface IRepeatableInteractable
{
    bool CanRepeatPrimary(
        PlayerInteraction player
    );

    bool CanRepeatSecondary(
        PlayerInteraction player
    );
}