namespace Telemart.Client.ViewModels.Common
{
    public sealed class UsageReasonParameter
    {
        public UsageReasonParameter(int? entityId, string featureName)
        {
            EntityId = entityId;
            FeatureName = featureName;
        }

        public int? EntityId { get; }

        public string FeatureName { get; }
    }
}
