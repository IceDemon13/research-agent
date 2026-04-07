namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class PosParameter
    {
        public PosParameter(int? legalEntityId, string uniqueDeviceId)
        {
            LegalEntityId = legalEntityId;
            UniqueDeviceId = uniqueDeviceId;
        }

        public int? LegalEntityId { get; }

        public string UniqueDeviceId { get; }
    }
}