using System.Threading.Tasks;
using DevExpress.DataProcessing;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Products.Content;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures
{
    public class CreateProductFeatureValueViewModel : TelemartDialogViewModelBase<CreateProductFeatureValueParameter, FeatureValueExDto>
    {
        public CreateProductFeatureValueViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Messenger = messenger;

            Title = "Создание значения характеристики";
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value, () => RaisePropertiesChanged(nameof(ValueUkr), nameof(ValueEn))); }
        }

        public string ValueUkr
        {
            get { return Parameter.MultiLanguage ? Value : GetProperty(() => ValueUkr); }
            set { SetProperty(() => ValueUkr, value); }
        }

        public string ValueEn
        {
            get { return Parameter.MultiLanguage ? Value : GetProperty(() => ValueEn); }
            set { SetProperty(() => ValueEn, value); }
        }

        public string Url
        {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value, () => RaisePropertiesChanged(nameof(UrlUkr), nameof(UrlEn))); }
        }

        public string UrlUkr
        {
            get { return Parameter.MultiLanguage ? Url : GetProperty(() => UrlUkr); }
            set { SetProperty(() => UrlUkr, value); }
        }

        public string UrlEn
        {
            get { return Parameter.MultiLanguage ? Url : GetProperty(() => UrlEn); }
            set { SetProperty(() => UrlEn, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateProductFeatureValueViewModel> builder)
        {
            builder.Property(x => x.Value)
                .MatchesInstanceRule(
                    (x, y) =>
                        (string.IsNullOrWhiteSpace(y.Parameter.Regex) && !string.IsNullOrWhiteSpace(x)) ||
                        (!string.IsNullOrWhiteSpace(x) && System.Text.RegularExpressions.Regex.IsMatch(x, y.Parameter.Regex)),
                    () => "Невалидное значение");

            builder.Property(x => x.ValueUkr)
                 .MatchesInstanceRule(
                     (x, y) =>
                         (string.IsNullOrWhiteSpace(y.Parameter.Regex) && !string.IsNullOrWhiteSpace(x)) ||
                         (!string.IsNullOrWhiteSpace(x) && System.Text.RegularExpressions.Regex.IsMatch(x, y.Parameter.Regex)),
                     () => "Невалидное значение");

            builder.Property(x => x.ValueEn)
                .MatchesInstanceRule(
                    (x, y) =>
                        (string.IsNullOrWhiteSpace(y.Parameter.Regex) && !string.IsNullOrWhiteSpace(x)) ||
                        (!string.IsNullOrWhiteSpace(x) && System.Text.RegularExpressions.Regex.IsMatch(x, y.Parameter.Regex)),
                    () => "Невалидное значение");
        }

        protected override async Task HandleOkAsync()
        {
            FeatureValueCreateDto featureValueCreateDto = new FeatureValueCreateDto(Parameter.FeatureId, Value, ValueUkr, ValueEn, Url, UrlUkr, UrlEn);

            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new CreateFeatureProductValue(featureValueCreateDto)), "создании значения", "Значение создано", this, true))
                .IfNotNull(x =>
                {
                    Messenger.Send(new EntityMessage<FeatureValueExDto>(x.Data, MessageType.Added));
                    SetResult(x.Data);
                    CloseOk();
                });
        }
    }
}
