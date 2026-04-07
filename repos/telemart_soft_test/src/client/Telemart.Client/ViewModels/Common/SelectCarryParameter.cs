namespace Telemart.Client.ViewModels.Common
{
    public sealed class SelectCarryParameter
    {
        public SelectCarryParameter(int[] ignoreCarryIds)
        {
            IgnoreCarryIds = ignoreCarryIds;
        }

        public int[] IgnoreCarryIds { get; }
    }
}