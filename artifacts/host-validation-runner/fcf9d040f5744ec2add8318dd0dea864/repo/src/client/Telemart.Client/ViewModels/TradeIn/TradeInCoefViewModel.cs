using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Requests.Features.TradeIn.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInCoefViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        public TradeInCoefViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;

            DeleteCommand = new DelegateCommand(Delete, () => SelectedCoef is not null);
        }

        public IDelegateCommand DeleteCommand { get; }

        public ObservableCollection<TraderInCoefViewItem> Coefs
        {
            get { return GetProperty(() => Coefs); }
            set { SetProperty(() => Coefs, value); }
        }

        public TraderInCoefViewItem SelectedCoef
        {
            get { return GetProperty(() => SelectedCoef); }
            set { SetProperty(() => SelectedCoef, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Indicators
        {
            get { return GetProperty(() => Indicators); }
            set { SetProperty(() => Indicators, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> IndicatorValues
        {
            get { return GetProperty(() => IndicatorValues); }
            set { SetProperty(() => IndicatorValues, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllCategories
        {
            get { return GetProperty(() => AllCategories); }
            set { SetProperty(() => AllCategories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ActualCategories
        {
            get { return GetProperty(() => ActualCategories); }
            set { SetProperty(() => ActualCategories, value); }
        }

        public bool AllowEditCoef
        {
            get { return GetProperty(() => AllowEditCoef); }
            set { SetProperty(() => AllowEditCoef, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            Task<List<TradeInIndicatorValueDto>> indicatorValuesTask = WebClient.ExecuteApiRequestAsync(new QueryTradeInIndicatorValues());
            Task<List<TradeInCoefDto>> coefsTask = WebClient.ExecuteApiRequestAsync(new QueryTradeInCoefs());
            Task<PagedResult<CategoryDto>> allCategoriesTask = WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            await Task.WhenAll(indicatorValuesTask, coefsTask, allCategoriesTask);

            AllowEditCoef = WebClient.IsOperationAllowed(BusinessOperation.UpdateTradeInSegment);

            AllCategories = allCategoriesTask.Result.Data
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ActualCategories = allCategoriesTask.Result.Data
                .Where(x => x.Active > 0 && x.UseInTradeIn)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Indicators = Dictionaries
                .GetItems<TradeInIndicator>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            IndicatorValues = indicatorValuesTask.Result
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            List<TraderInCoefViewItem> coefs = coefsTask.Result.Select(x => new TraderInCoefViewItem()
            {
                Id = x.Id,
                Coef = x.Coef,
                CategoryId = x.CategoryId,
                IndicatorValueId = x.IndicatorValueId,
                IndicatorId = x.IndicatorId
            }).ToList();

            foreach (ComboBoxItem actualCategory in ActualCategories)
            {
                TraderInCoefViewItem[] categoryCoefs = coefs.Where(x => x.CategoryId == actualCategory.Id).ToArray();

                if (categoryCoefs.Any())
                {
                    foreach (TradeInIndicatorValueDto indicatorValue in indicatorValuesTask.Result)
                    {
                        if (categoryCoefs.All(x => x.IndicatorValueId != indicatorValue.Id))
                        {
                            coefs.Add(new TraderInCoefViewItem()
                            {
                                Id = 0,
                                Coef = null,
                                CategoryId = actualCategory.Id,
                                IndicatorId = indicatorValue.IndicatorId,
                                IndicatorValueId = indicatorValue.Id
                            });
                        }
                    }
                }
                else
                {
                    coefs.AddRange(indicatorValuesTask.Result.Select(x => new TraderInCoefViewItem()
                    {
                        Id = 0,
                        CategoryId = actualCategory.Id,
                        Coef = null,
                        IndicatorId = x.IndicatorId,
                        IndicatorValueId = x.Id
                    }));
                }
            }

            Coefs = coefs.ToObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Настройка коэффициентов ценообразования Trade-In";
        }

        protected override async Task HandleOkAsync()
        {
            if (Coefs.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationError("В таблице присутствуют ошибки");
                return;
            }

            TradeInCoefDto[] updateDtos = Coefs.Select(x => new TradeInCoefDto()
            {
                Id = x.Id,
                Coef = x.Coef!.Value,
                CategoryId = x.CategoryId,
                IndicatorValueId = x.IndicatorValueId
            }).ToArray();

            Result<object> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateTradeInCoefs(updateDtos)),
                "сохранении коэффициентов",
                "Коэффициенты сохранены",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private void Delete()
        {
            Coefs.Remove(SelectedCoef);
            SelectedCoef = null;
        }
    }
}