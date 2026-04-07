using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.PriceCalculation;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ProductPricesRobotViewModel : TelemartDialogViewModelBase
    {
        private ProductPricesViewModel parent;

        public ProductPricesRobotViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DebugCommand = new AsyncCommand(DebugAsync);
            SaveCommand = new AsyncCommand(SaveAsync);
        }

        public ProductPricesRobotViewModel()
        {
        }

        #region Commands

        public IAsyncCommand DebugCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        #endregion

        #region INPC

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
        }

        public int ProductCategoryId
        {
            get { return GetProperty(() => ProductCategoryId); }
            private set { SetProperty(() => ProductCategoryId, value); }
        }

        public string ProductCategoryName
        {
            get { return GetProperty(() => ProductCategoryName); }
            private set { SetProperty(() => ProductCategoryName, value); }
        }

        public string RobotScript
        {
            get { return GetProperty(() => RobotScript); }
            set { SetProperty(() => RobotScript, value, () => RaisePropertyChanged(nameof(RobotScriptFull))); }
        }

        public string RobotScriptParameters
        {
            get { return GetProperty(() => RobotScriptParameters); }
            set { SetProperty(() => RobotScriptParameters, value, () => RaisePropertyChanged(nameof(RobotScriptFull))); }
        }

        public string RobotScriptFull => RobotHelper.GetFullScript(RobotScript, RobotScriptParameters);

        public PriceRobotMode RobotMode
        {
            get { return GetProperty(() => RobotMode); }
            set { SetProperty(() => RobotMode, value); }
        }

        public bool UseScriptFromCategory
        {
            get { return GetProperty(() => UseScriptFromCategory); }
            set { SetProperty(() => UseScriptFromCategory, value, () => UseScriptFromCategoryChangedCallbackAsync()); }
        }

        public bool UseRobotModeFromProduct
        {
            get { return GetProperty(() => UseRobotModeFromProduct); }
            set { SetProperty(() => UseRobotModeFromProduct, value, UseRobotModeFromProductChangedCallback); }
        }

        public ReadOnlyObservableCollection<PriceRobotMode> RobotModes
        {
            get { return GetProperty(() => RobotModes); }
            private set { SetProperty(() => RobotModes, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 480;

        public override int MinWidth => 640;

        public override int Width => 800;

        #endregion

        public async Task RefreshAsync()
        {
            ProductPriceViewItem currentProduct = parent.CurrentProduct;

            ProductName = currentProduct.Name;
            ProductCategoryId = currentProduct.CategoryId;
            ProductCategoryName = currentProduct.CategoryName;

            (string Script, string Parameters) robotScript = await parent.GetRobotScriptByCategoryAsync(currentProduct.CategoryId);

            if (UseScriptFromCategory)
            {
                RobotScript = robotScript.Script;
                RobotScriptParameters = robotScript.Parameters;
            }

            if (UseRobotModeFromProduct)
            {
                RobotMode = currentProduct.RobotModeManual;
            }
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            RobotModes = new[] { PriceRobotMode.No, PriceRobotMode.AvailMinus, PriceRobotMode.AvailPlus, PriceRobotMode.Price }.ToReadOnlyObservableCollection();

            ProductName = "Samsung Galaxy S8";
            ProductCategoryName = "Samsung";
            UseRobotModeFromProduct = true;
            UseScriptFromCategory = true;
            RobotScript = "Console.WriteLine(\"Hello world !\")";
            RobotMode = PriceRobotMode.AvailPlus;
        }

        protected override void OnParentViewModelChanged(object parentViewModel)
        {
            base.OnParentViewModelChanged(parentViewModel);

            parent = (ProductPricesViewModel)parentViewModel;
        }

        protected override Task HandleLoadedAsync()
        {
            RobotModes = Dictionaries.GetItems<PriceRobotMode>().ToReadOnlyObservableCollection();

            UseScriptFromCategory = true;
            UseRobotModeFromProduct = true;

            Title = "Вычислить цены и наличие";

            return Task.CompletedTask;
        }

        protected override void HandleCancel()
        {
            if (DocumentOwner != null)
            {
                base.HandleCancel();
            }
        }

        protected override Task HandleOkAsync()
        {
            return parent.OldCalculatePricesAsync(
                DialogDocumentManagerService,
                UseRobotModeFromProduct,
                UseScriptFromCategory,
                RobotMode,
                RobotScriptFull);
        }

        private async Task DebugAsync()
        {
            (string debug, CalculatePriceResult result) = await parent.DebugAsync(RobotMode, RobotScriptFull, true);

            if (result != null)
            {
                SizeableDialogDocumentManagerService.ShowView<ProductPricesDebugViewModel>(
                    new object[] { debug, result.Errors },
                    this);
            }
        }

        private async Task SaveAsync()
        {
            if (UseScriptFromCategory)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            (_, CalculatePriceResult result) = await parent.DebugAsync(RobotMode, RobotScriptFull, false);

            if (result == null)
            {
                return;
            }

            if (result.Errors?.Any() == true)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при выполнении скрипта", result.Errors.Select(x => new ValidationResultItem(x, true)), this);
                return;
            }

            if (!MessageFacadeService.Confirm($"Сохранить текущий скрипт для категории \"{ProductCategoryName}\"?"))
            {
                return;
            }

            try
            {
                UpdateCategoryRobotScript request = new UpdateCategoryRobotScript(ProductCategoryId, RobotScript, RobotScriptParameters);

                await WebClient.ExecuteApiRequestAsync(request);

                parent.SetRobotScript(ProductCategoryId, RobotScript, RobotScriptParameters);

                MessageFacadeService.ShowNotificationInfo("Скрипт успешно сохранен");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                ShowValidationResultView("Ошибки при сохранении", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save robot script");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving robot script");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }

        private async Task UseScriptFromCategoryChangedCallbackAsync()
        {
            if (IsInDesignMode)
            {
                return;
            }

            if (UseScriptFromCategory)
            {
                (string Script, string Parameters) robotScript = await parent.GetRobotScriptByCategoryAsync(parent.CurrentProduct.CategoryId);

                RobotScript = robotScript.Script;
                RobotScriptParameters = robotScript.Parameters;
            }
        }

        private void UseRobotModeFromProductChangedCallback()
        {
            if (IsInDesignMode)
            {
                return;
            }

            if (UseRobotModeFromProduct)
            {
                RobotMode = parent.CurrentProduct.RobotModeManual;
            }
        }
    }
}