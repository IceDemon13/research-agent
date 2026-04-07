using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ConstantQueries;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.Requests.Features.Payments.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;
using Telemart.Common.TransferObjects;

namespace Telemart.Client.ViewModels.Payments
{
    public sealed class CreditOffersViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IMapper _mapper;

        public CreditOffersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            _mapper = mapper;

            AddCreditOfferCommand = new DelegateCommand(AddCreditOffer, () => !ReadOnlyMode);
        }

        public IDelegateCommand AddCreditOfferCommand { get; }

        public ObservableCollection<CreditOfferViewItem> CreditOffers
        {
            get { return GetProperty(() => CreditOffers); }
            set { SetProperty(() => CreditOffers, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CreditPayments
        {
            get { return GetProperty(() => CreditPayments); }
            private set { SetProperty(() => CreditPayments, value); }
        }

        public decimal MinProductMarginPercent
        {
            get { return GetProperty(() => MinProductMarginPercent); }
            set { SetProperty(() => MinProductMarginPercent, value); }
        }

        public int? MinPartialPayCount
        {
            get { return GetProperty(() => MinPartialPayCount); }
            set { SetProperty(() => MinPartialPayCount, value); }
        }

        public bool ReadOnlyMode
        {
            get { return GetProperty(() => ReadOnlyMode); }
            set { SetProperty(() => ReadOnlyMode, value); }
        }

        public bool MinProductMarginPercentReadOnlyMode
        {
            get { return GetProperty(() => MinProductMarginPercentReadOnlyMode); }
            set { SetProperty(() => MinProductMarginPercentReadOnlyMode, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CreditOffersViewModel> builder)
        {
            builder.Property(x => x.MinProductMarginPercent)
                .MatchesRule(x => x is >= 0 and < 100, () => "Значение должно быть больше или равно 0 и меньше 100");

            builder.Property(x => x.MinPartialPayCount)
                .MatchesRule(x => x is null or > 0 and < 24, () => "Значение должно быть больше 0 и меньше 24");
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CreditOfferDto> creditOffers = await WebClient.ExecuteApiRequestAsync(new QueryCreditOffers());

            CreditOffers = new ObservableCollection<CreditOfferViewItem>(_mapper.Map<List<CreditOfferViewItem>>(creditOffers));

            object minProductMarginPercentObj = await WebClient.ExecuteApiRequestAsync(new QueryConstant(ConstantKeys.MinProductMarginPercent));

            if (decimal.TryParse(minProductMarginPercentObj.ToString(), out decimal minProductMarginPercent))
            {
                MinProductMarginPercent = minProductMarginPercent;
            }
            else
            {
                MinProductMarginPercent = 0;
            }

            object minPartialPayCountObj = await WebClient.ExecuteApiRequestAsync(new QueryConstant(ConstantKeys.MinPartialPayCount));

            if (int.TryParse(minPartialPayCountObj.ToString(), out int minPartialPayCount))
            {
                if (minPartialPayCount == 0)
                {
                    MinPartialPayCount = null;
                }
                else
                {
                    MinPartialPayCount = minPartialPayCount;
                }
            }
            else
            {
                MinPartialPayCount = null;
            }

            CreditPayments = Dictionaries
                .GetItems<Payment>()
                .Where(x => x.Credit)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            ReadOnlyMode = !WebClient.IsOperationAllowed(BusinessOperation.CreditOfferUpdate);
            MinProductMarginPercentReadOnlyMode = !WebClient.IsOperationAllowed(BusinessOperation.CreditOfferMinProductMarginPercentUpdate);

            await base.HandleLoadedAsync();

            Title = "Кредитные предложения";
        }

        protected override async Task HandleOkAsync()
        {
            if (ReadOnlyMode && MinProductMarginPercentReadOnlyMode)
            {
                CloseOk();
                return;
            }

            if (!ReadOnlyMode && CreditOffers.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationError("Не все поля заполнены корректно");
                return;
            }

            CreditOffersSaveDto creditOffersSaveDto = new CreditOffersSaveDto
            {
                MinProductMarginPercent = MinProductMarginPercent,
                MinPartialPayCount = MinPartialPayCount,
                CreditOffers = _mapper.Map<List<CreditOfferDto>>(CreditOffers)
            };

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SaveCreditOffers(creditOffersSaveDto)),
                "сохранении",
                "Кредитные предлоджения сохранены",
                this,
                true,
                showDialog: true,
                showError: true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private void AddCreditOffer()
        {
            CreditOffers.Insert(0, new CreditOfferViewItem());
        }
    }
}