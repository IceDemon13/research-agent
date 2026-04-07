namespace Telemart.Client.ViewModels.Common
{
    public sealed class ChangeCarryParameter
    {
        public ChangeCarryParameter(int carryId, int[] ignoreCarryIds)
        {
            CarryId = carryId;
            IgnoreCarryIds = ignoreCarryIds;
        }

        public int CarryId { get; }

        public int[] IgnoreCarryIds { get; }
    }
}