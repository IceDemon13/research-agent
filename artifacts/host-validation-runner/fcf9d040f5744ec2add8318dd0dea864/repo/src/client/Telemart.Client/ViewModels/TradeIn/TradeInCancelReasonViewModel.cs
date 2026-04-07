using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInCancelReasonViewModel : TelemartDialogViewModelBase
    {
        private IErrorHandler _errorHandler;
        private IMapper _mapper;

        public TradeInCancelReasonViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            _mapper = mapper;
        }

        public ReadOnlyObservableCollection<TradeInReasonCancelTypeViewItem> ReasonCancelTypes
        {
            get { return GetProperty(() => ReasonCancelTypes); }
            set { SetProperty(() => ReasonCancelTypes, value); }
        }

        public TradeInReasonCancelTypeViewItem ReasonCancelType
        {
            get { return GetProperty(() => ReasonCancelType); }
            set { SetProperty(() => ReasonCancelType, value); }
        }

        public static void BuildMetadata(MetadataBuilder<TradeInCancelReasonViewModel> builder)
        {
            builder.Property(x => x.ReasonCancelType)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            Title = "Укажите причину отмены Trade-In заявки";

            await LoadCancelReasonsAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private async Task LoadCancelReasonsAsync()
        {
            List<TradeInCancelReasonDto> reasons = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryTradeInCancelReasons()),
                "при получении причин отмены",
                null,
                this,
                false,
                showNotification: false);

            ReasonCancelTypes = reasons.Select(x => _mapper.Map<TradeInReasonCancelTypeViewItem>(x)).ToReadOnlyObservableCollection();
        }
    }
}