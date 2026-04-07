using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Cities
{
    public class ForeignCityCreateViewModel : TelemartDialogViewModelBase
    {
        private int? countryId;

        public ForeignCityCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            ErrorHandler = errorHandler;

            Title = "Создание города";
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<ForeignCityCreateViewModel> builder)
        {
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUkr).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            countryId = (int?)Parameter;

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (countryId.HasValue)
            {
                Result<ForeignCityDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreateForeignCity(Name, NameUkr, NameEn, countryId.Value)),
                    "создании города",
                    "город создан",
                    this,
                    true);

                if (result.IsSuccess)
                {
                    Messenger.Send(new ForeignCityMessage(result.Data, MessageType.Added));

                    CloseOk();
                }
            }
        }
    }
}