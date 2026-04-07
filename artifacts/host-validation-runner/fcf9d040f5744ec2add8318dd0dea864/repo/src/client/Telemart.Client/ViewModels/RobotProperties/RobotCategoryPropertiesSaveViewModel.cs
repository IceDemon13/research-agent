using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.RobotProperty;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.RobotProperty;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public sealed class RobotCategoryPropertiesSaveViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private int _productTypeId;

        private ObservableCollection<RobotCategoryPropertyValueSaveViewItem> _notChangedRobotSaveCategoryPropertyValues;

        public RobotCategoryPropertiesSaveViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;

            RobotCategoryPropertyValueSaveChangedCommand = new DelegateCommand(RobotCategoryPropertyValueSaveChanged);

            _notChangedRobotSaveCategoryPropertyValues = new ObservableCollection<RobotCategoryPropertyValueSaveViewItem>();
        }

        public IDelegateCommand RobotCategoryPropertyValueSaveChangedCommand { get; }

        #region INPC

        public string CategoryFullName
        {
            get { return GetProperty(() => CategoryFullName); }
            private set { SetProperty(() => CategoryFullName, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            private set { SetProperty(() => CategoryId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            private set { SetProperty(() => ProductTypes, value); }
        }

        public int? SelectedProductTypeId
        {
            get { return GetProperty(() => SelectedProductTypeId); }
            set { SetProperty(() => SelectedProductTypeId, value); }
        }

        public bool SaveForAllProductTypes
        {
            get { return GetProperty(() => SaveForAllProductTypes); }
            set { SetProperty(() => SaveForAllProductTypes, value, OnSaveForAllProductTypesChanged); }
        }

        public ReadOnlyObservableCollection<RobotCategoryPropertyValueSaveType> RobotCategoryPropertyValueSaveTypes
        {
            get { return GetProperty(() => RobotCategoryPropertyValueSaveTypes); }
            set { SetProperty(() => RobotCategoryPropertyValueSaveTypes, value); }
        }

        public RobotCategoryPropertyValueSaveType SelectedRobotCategoryPropertyValueSaveType
        {
            get { return GetProperty(() => SelectedRobotCategoryPropertyValueSaveType); }
            set { SetProperty(() => SelectedRobotCategoryPropertyValueSaveType, value, RobotCategoryPropertyValueSaveChanged); }
        }

        public ReadOnlyObservableCollection<RobotCategoryPropertyValueSaveViewItem> RobotSaveCategoryPropertyValues
        {
            get { return GetProperty(() => RobotSaveCategoryPropertyValues); }
            set { SetProperty(() => RobotSaveCategoryPropertyValues, value); }
        }

        public RobotCategoryPropertyValueSaveViewItem SelectedRobotSaveCategoryPropertyValue
        {
            get { return GetProperty(() => SelectedRobotSaveCategoryPropertyValue); }
            set { SetProperty(() => SelectedRobotSaveCategoryPropertyValue, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        #endregion

        protected override Task HandleLoadedAsync()
        {
            RobotCategoryPropertiesSaveParameter parameter = (RobotCategoryPropertiesSaveParameter)Parameter;

            _productTypeId = parameter.ProductTypeId;
            SelectedProductTypeId = parameter.ProductTypeId;
            CategoryId = parameter.CategoryId;
            CategoryFullName = parameter.CategoryName;

            ProductTypes = Dictionaries
                .GetItems<ProductType>()
                .Where(x => !x.IsVirtual)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            RobotSaveCategoryPropertyValues = parameter.Values
                .Where(x => x.IsChanged)
                .ToReadOnlyObservableCollection();

            _notChangedRobotSaveCategoryPropertyValues = parameter.Values
                .Where(x => !x.IsChanged).ToObservableCollection();

            List<RobotCategoryPropertyValueSaveType> robotCategoryPropertyValueSaveTypes = Dictionaries.GetItems<RobotCategoryPropertyValueSaveType>().ToList();

            if (!WebClient.IsOperationAllowed(BusinessOperation.RobotCategoryPropertyValuesAllowEditAllCategories))
            {
                robotCategoryPropertyValueSaveTypes.Remove(RobotCategoryPropertyValueSaveType.AllCategories);
            }

            if (!WebClient.IsOperationAllowed(BusinessOperation.RobotCategoryPropertyValuesAllowEditEmployeesCategories))
            {
                robotCategoryPropertyValueSaveTypes.Remove(RobotCategoryPropertyValueSaveType.EmployeesCategories);
            }

            RobotCategoryPropertyValueSaveTypes = robotCategoryPropertyValueSaveTypes.ToReadOnlyObservableCollection();

            Title = $"Сохранение {CategoryFullName}({CategoryId})";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            await SaveAsync();

            if (IsOk)
            {
                Close();
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                IsLongOperationInProgress = true;

                List<RobotCategoryPropertyValueSaveViewItem> valueSaveViewItems = _notChangedRobotSaveCategoryPropertyValues.Union(RobotSaveCategoryPropertyValues).ToList();

                List<RobotCategoryPropertyValueSaveDto> valueSaveDtos = valueSaveViewItems
                    .Select(x => new RobotCategoryPropertyValueSaveDto(x.Id, x.PropertyId, x.CategoryPropertyValueNew, x.ActiveNew, x.ValueSaveTypeId, x.IsChanged))
                    .ToList();

                SaveRobotCategoryPropertyValuesDto saveCategoryValueDtos = new SaveRobotCategoryPropertyValuesDto(CategoryId, SelectedProductTypeId, valueSaveDtos);

                Result result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new SaveRobotCategoryPropertyValues(saveCategoryValueDtos)),
                    "получении переменных робота",
                    "Настройки робота для категории сохранены",
                    this,
                    true,
                    confirmText: "Сохранить настройки робота");

                if (result?.IsSuccess == true)
                {
                    CloseOk();
                }
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private void RobotCategoryPropertyValueSaveChanged()
        {
            RobotSaveCategoryPropertyValues.ForEach(x => x.ValueSaveTypeId = SelectedRobotCategoryPropertyValueSaveType.Id);
        }

        private void OnSaveForAllProductTypesChanged()
        {
            if (SaveForAllProductTypes)
            {
               SelectedProductTypeId = null;
            }
            else
            {
                SelectedProductTypeId = _productTypeId;
            }
        }
    }
}