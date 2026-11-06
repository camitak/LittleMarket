public enum FurnitureAccessRequirement
{
    /*
     * Keep this as value 0.
     *
     * Existing FurnitureDefinition assets that
     * pre-date this field therefore preserve their
     * current front-access behaviour automatically.
     */
    FrontOperational = 0,

    None = 1
}