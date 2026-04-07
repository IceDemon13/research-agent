using System.Globalization;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using SmartFormat;
using Telemart.Client.Business;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInEvaluateViewModel : TelemartDialogViewModelBase
    {
        private TradeInEvaluateParameter _parameter;
        private string _olxSearchUrl;
        private string _overclockersSearchUrl;

        public TradeInEvaluateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            OlxCommand = new DelegateCommand(OlxSearch, () => _parameter != null && (!string.IsNullOrWhiteSpace(_parameter.Brand) || !string.IsNullOrEmpty(_parameter.PnModel)));
            OverclockersCommand = new DelegateCommand(OverclockersSearch, () => _parameter != null && (!string.IsNullOrWhiteSpace(_parameter.Brand) || !string.IsNullOrEmpty(_parameter.PnModel)));
        }

        public string LastEvaluate
        {
            get { return GetProperty(() => LastEvaluate); }
            set { SetProperty(() => LastEvaluate, value); }
        }

        public string LastDateEvaluate
        {
            get { return GetProperty(() => LastDateEvaluate); }
            set { SetProperty(() => LastDateEvaluate, value); }
        }

        public decimal? MaxEvaluate
        {
            get { return GetProperty(() => MaxEvaluate); }
            set { SetProperty(() => MaxEvaluate, value, ChangeMaxPrice); }
        }

        public bool NotifyClient
        {
            get { return GetProperty(() => NotifyClient); }
            set { SetProperty(() => NotifyClient, value); }
        }

        public IDelegateCommand OlxCommand { get; }

        public IDelegateCommand OverclockersCommand { get; }

        public static void BuildMetadata(MetadataBuilder<TradeInEvaluateViewModel> builder)
        {
            builder.Property(x => x.MaxEvaluate)
                .MatchesRule(x => x > 0, () => "Допустимые значения 1...999999.99");
        }

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (TradeInEvaluateParameter)Parameter;

            LastEvaluate = _parameter.MaxPrice?.ToString("F2", CultureInfo.InvariantCulture);

            LastDateEvaluate = _parameter.DateMaxPrice?.ToString(DateFormattingRules.FullDateTimeFormat);

            await Task.WhenAll(LoadOlxSearchUrlAsync(), LoadOverclockersSearchUrlAsync());

            NotifyClient = !(_parameter.MaxPrice.HasValue && _parameter.DateMaxPrice.HasValue);

            Title = "Оценка";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private async Task LoadOlxSearchUrlAsync()
        {
            _olxSearchUrl = await WebClient.ExecuteApiRequestAsync(new QueryOlxSearchUrl());
        }

        private async Task LoadOverclockersSearchUrlAsync()
        {
            _overclockersSearchUrl = await WebClient.ExecuteApiRequestAsync(new QueryOverclockersSearchUrl());
        }

        private void OlxSearch()
        {
            string brand = _parameter.Brand?.Replace(' ', '-');
            string pnModel = _parameter.PnModel?.Replace(' ', '-');

            var data = new { TradeInProduct = $"{brand}-{pnModel}" };

            string fullUrl = Smart.Format(_olxSearchUrl, data);

            ProcessHelper.Start(fullUrl);
        }

        private void OverclockersSearch()
        {
            string brand = _parameter.Brand?.Replace(' ', '+');
            string pnModel = _parameter.PnModel?.Replace(' ', '+');

            var data = new { TradeInProduct = $"{brand}-{pnModel}" };

            string fullUrl = Smart.Format(_overclockersSearchUrl, data);

            ProcessHelper.Start(fullUrl);
        }

        private void ChangeMaxPrice()
        {
            if (_parameter.MaxPrice.HasValue && _parameter.DateMaxPrice.HasValue)
            {
                NotifyClient = _parameter.MaxPrice != MaxEvaluate;
            }
        }
    }
}