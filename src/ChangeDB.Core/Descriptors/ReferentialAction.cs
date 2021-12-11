namespace ChangeDB
{
    public enum ReferentialAction
    {
        //
        // Summary:
        //     Do nothing. That is, just ignore the constraint.
        NoAction = 0,
        //
        // Summary:
        //     Don't perform the action if it would result in a constraint violation and instead
        //     generate an error.
        Restrict = 1,
        //
        // Summary:
        //     Cascade the action to the constrained rows.
        Cascade = 2,
        //
        // Summary:
        //     Set null on the constrained rows so that the constraint is not violated after
        //     the action completes.
        SetNull = 3,
        //
        // Summary:
        //     Set a default value on the constrained rows so that the constraint is not violated
        //     after the action completes.
        SetDefault = 4
    }
}
