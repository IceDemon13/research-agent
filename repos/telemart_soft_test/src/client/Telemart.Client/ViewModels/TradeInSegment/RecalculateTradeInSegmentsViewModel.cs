using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.TradeInSegment.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeInSegment;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public class RecalculateTradeInSegmentsViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        public RecalculateTradeInSegmentsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this._errorHandler = errorHandler;

            SelectedParentCategories = new ObservableCollection<CategoryDto>();
        }

        public ReadOnlyObservableCollection<CategoryDto> ParentCategories
        {
            get { return GetProperty(() => ParentCategories); }
            private set { SetProperty(() => ParentCategories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<CategoryDto> SelectedParentCategories
        {
            get { return GetProperty(() => SelectedParentCategories); }
            set { SetProperty(() => SelectedParentCategories, value); }
        }

        public ObservableCollection<RecalculateTradeInSegmentsViewItem> ResultCategories
        {
            get { return GetProperty(() => ResultCategories); }
            private set { SetProperty(() => ResultCategories, value); }
        }

        public bool ShowResultGrid
        {
            get { return GetProperty(() => ShowResultGrid); }
            private set { SetProperty(() => ShowResultGrid, value); }
        }

        public string OkButtonText
        {
            get { return GetProperty(() => OkButtonText); }
            private set { SetProperty(() => OkButtonText, value); }
        }

        #region DialogSettings

        public override int Width => 700;

        public override int MinWidth => 500;

        public override int MaxWidth => 1000;

        public override int Height => 600;

        public override int MinHeight => 400;

        public override int MaxHeight => 800;

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            OkButtonText = "Рассчитать";

            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            Employees = employees
                .Data
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ParentCategories = categories
                .Data
                .Where(x => x.Active == 1 && x.IsParent)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Пересчет товаров по сегментам Trade-In";
        }

        protected override async Task HandleOkAsync()
        {
            if (ShowResultGrid)
            {
                CloseOk();
            }
            else
            {
                if (!SelectedParentCategories.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Ничего не выбрано");
                    return;
                }

                DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>("Вы уверены что хотите пересчитать товары по сегментам?", this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                (await _errorHandler.HandleErrorsAsync(ct => WebClient.ExecuteApiRequestAsync(new RecalculateTradeInSegments(new RecalculateTradeInSegmentsDto(SelectedParentCategories.Select(x => x.Id).ToArray()))), "Пересчете сегментов", "Товары пересчитаны", this, true))
                    .IfNotNull(z =>
                    {
                        ResultCategories = z.Data.ParentCategories
                            .Select(x => new RecalculateTradeInSegmentsViewItem(x.ParentCategoryId, x.ParentCategoryName, x.TradeInSegmentId, x.TradeInSegmentName, x.CalculatedProductsQuantity))
                            .ToObservableCollection();

                        var noSegmentGroups = z.Data.ParentCategories
                            .GroupBy(x => new { x.ParentCategoryId, x.ParentCategoryName, x.TotalCategoryProductsQuantity, x.ProductsQuantityWithNotFilledFeaturesFromParentCategorySegments })
                            .Where(x => x.Sum(r => r.CalculatedProductsQuantity) < x.Key.TotalCategoryProductsQuantity);

                        foreach (var noSegmentGroup in noSegmentGroups)
                        {
                            int calculatedProductsQuantity = noSegmentGroup.Sum(x => x.CalculatedProductsQuantity);

                            if (noSegmentGroup.Key.ProductsQuantityWithNotFilledFeaturesFromParentCategorySegments > 0)
                            {
                                ResultCategories.Add(new RecalculateTradeInSegmentsViewItem(
                                    noSegmentGroup.Key.ParentCategoryId,
                                    noSegmentGroup.Key.ParentCategoryName,
                                    0,
                                    "Без сегмента (не заполнены характеристики)",
                                    noSegmentGroup.Key.ProductsQuantityWithNotFilledFeaturesFromParentCategorySegments,
                                    false,
                                    true));
                            }

                            int noSegmentQuantity = noSegmentGroup.Key.TotalCategoryProductsQuantity - noSegmentGroup.Key.ProductsQuantityWithNotFilledFeaturesFromParentCategorySegments - calculatedProductsQuantity;

                            if (noSegmentQuantity > 0)
                            {
                                ResultCategories.Add(new RecalculateTradeInSegmentsViewItem(
                                    noSegmentGroup.Key.ParentCategoryId,
                                    noSegmentGroup.Key.ParentCategoryName,
                                    0,
                                    "Без сегмента (не соответствует ни одному сегменту)",
                                    noSegmentQuantity,
                                    true));
                            }
                        }

                        ResultCategories = ResultCategories.OrderBy(x => x.ParentCategoryId).ToObservableCollection();

                        ShowResultGrid = true;
                        OkButtonText = "OK";
                    });
            }
        }
    }
}