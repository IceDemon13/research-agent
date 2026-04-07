namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class EditIpAddressParameter
    {
        public EditIpAddressParameter(string ipAddress, string macAddress)
        {
            IpAddress = ipAddress;
            MacAddress = macAddress;
        }

        public string IpAddress { get; }

        public string MacAddress { get; }
    }
}