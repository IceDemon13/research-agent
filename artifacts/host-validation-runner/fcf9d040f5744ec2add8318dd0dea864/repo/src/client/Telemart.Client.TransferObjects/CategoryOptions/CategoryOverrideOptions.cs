namespace Telemart.Client.TransferObjects.CategoryOptions
{
    public enum CategoryOverrideOptions
    {
        None = 0,
        OverrideOnlyInCategory = 1,
        OverrideInDescendantsWithSameValue = 2,
        OverrideInAllDescendants = 3
    }
}