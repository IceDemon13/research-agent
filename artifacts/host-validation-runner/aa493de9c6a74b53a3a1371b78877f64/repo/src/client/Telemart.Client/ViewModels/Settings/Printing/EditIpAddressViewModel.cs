using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class EditIpAddressViewModel : TelemartDialogViewModelBase
    {
        private EditIpAddressParameter _parameter;

        public EditIpAddressViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public string IpAddress
        {
            get { return GetProperty(() => IpAddress); }
            set { SetProperty(() => IpAddress, value, OnFiscalRegistrarIpChanged); }
        }

        public string Port
        {
            get { return GetProperty(() => Port); }
            set { SetProperty(() => Port, value); }
        }

        public string MacAddress
        {
            get { return GetProperty(() => MacAddress); }
            set { SetProperty(() => MacAddress, value); }
        }

        public static void BuildMetadata(MetadataBuilder<EditIpAddressViewModel> builder)
        {
            builder.Property(x => x.IpAddress)
                .MatchesRule(x => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.MacAddress)
                .MatchesInstanceRule((x, y) => string.IsNullOrEmpty(y.IpAddress) || !string.IsNullOrEmpty(x), () => "Неопределен МАС-адрес по IP-адресу");
        }

        protected override Task HandleLoadedAsync()
        {
            _parameter = (EditIpAddressParameter)Parameter;

            IPEndPoint.TryParse(_parameter.IpAddress, out IPEndPoint ipAddress);

            IpAddress = ipAddress?.Address.ToString();

            Port = ipAddress?.Port == 0 ? string.Empty : ipAddress?.Port.ToString();

            Title = "Изменение IP адреса";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (string.IsNullOrEmpty(_parameter?.MacAddress) || _parameter?.MacAddress == MacAddress)
            {
                CloseOk();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Не соответсвует МАС-адрес");
            }

            return Task.CompletedTask;
        }

        private void OnFiscalRegistrarIpChanged()
        {
            MacAddress = GetMac();

            RaisePropertiesChanged(nameof(MacAddress));
        }

        private string GetMac()
        {
            if (IPAddress.TryParse(IpAddress, out IPAddress ipAddress))
            {
                string mac = ipAddress.GetMacByIp();

                if (mac == "00-00-00-00-00-00")
                {
                    return null;
                }

                return mac;
            }

            return null;
        }
    }
}