namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class FiscalRegistrarParameter
    {
        public FiscalRegistrarParameter(string uniqueDeviceId)
        {
            UniqueDeviceId = uniqueDeviceId;
        }

        public string UniqueDeviceId { get; }
    }
}