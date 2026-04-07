using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Newtonsoft.Json;
using Telemart.Client.Common.CustomTypeDescriptors;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.RobotProperty;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Style.Editors;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.RobotProperty;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public sealed class RobotCategoryPropertyViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        private List<RobotPropertyDto> _allProperties;
        private List<RobotCategoryPropertyValueDto> _robotCategoryPropertyValueDtos;
        private int _oldProductTypeId;

        public RobotCategoryPropertyViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IErrorHandler errorHandler,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        #region INPC

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            private set { SetProperty(() => CategoryName, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            private set { SetProperty(() => CategoryId, value); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value, OnProductTypeChanged); }
        }

        public ReadOnlyObservableCollection<RobotPropertyType> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            private set { SetProperty(() => ProductTypes, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public CollectionTypeDescriptor PropertyGridSource
        {
            get { return GetProperty(() => PropertyGridSource); }
            set { SetProperty(() => PropertyGridSource, value); }
        }

        public bool IsChangedColumnVisible => WebClient.IsOperationAllowed(BusinessOperation.RobotCategoryPropertyValuesAllowEdit);

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            Types = Dictionaries.GetItems<RobotPropertyType>().ToReadOnlyObservableCollection();

            RobotCategoryParameter parameter = (RobotCategoryParameter)Parameter;

            CategoryName = parameter.FullName;
            CategoryId = parameter.CategoryId;
            ProductTypes = Dictionaries
                .GetItems<ProductType>()
                .Where(x => !x.IsVirtual)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            _robotCategoryPropertyValueDtos = await LoadCategoryPropertyValueAsync(parameter.CategoryId);

            await Task.WhenAll(LoadEmployeesAsync(), LoadPropertiesAsync());

            await base.HandleLoadedAsync();

            ProductTypeId = ProductType.ProductId;

            Title = $"Настройка робота категории {CategoryName}({CategoryId})";
        }

        protected override Task HandleOkAsync()
        {
            CollectionTypeDescriptor collectionTypeDescriptor = PropertyGridSource;

            IReadOnlyCollection<PropertyGridRow> rows = collectionTypeDescriptor.Rows;

            if (rows?.Any(x => x.Value is RobotCategoryPropertyValueContentViewItem item && (item.RobotCategoryValue?.IsChanged == true || item.Active != item.ActiveNew)) == true)
            {
                RobotCategoryPropertyValueContentViewItem[] itemsForSave = rows
                    .Where(x => x.Value is RobotCategoryPropertyValueContentViewItem item && (item.RobotCategoryValue.IsChanged || item.Required || item.CategoryId > 0) && item.ProductTypeId == ProductTypeId)
                    .Select(x => (RobotCategoryPropertyValueContentViewItem)x.Value)
                    .ToArray();

                RobotCategoryPropertyValueResult[] itemForSaveResult = itemsForSave.Select(
                    x => new RobotCategoryPropertyValueResult(
                        x,
                        GetValue(x.RobotPropertyType.Id, x.RobotCategoryValueOld),
                        GetValue(x.RobotPropertyType.Id, x.RobotCategoryValue),
                        x.RobotCategoryValue.IsChanged))
                    .ToArray();

                ValidationResultItem[] errors = CanSave(itemForSaveResult)?.ToArray();

                if (errors?.Any() == true)
                {
                    SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                        new ValidationResultViewModelParameter("Ошибки сохранения настроек робота", errors),
                        this);

                    return Task.CompletedTask;
                }

                ReadOnlyObservableCollection<RobotCategoryPropertyValueSaveViewItem> saveValues = itemForSaveResult
                    .Select(MapToRobotCategoryPropertyValueSaveViewItem)
                    .ToReadOnlyObservableCollection();

                if (saveValues?.Count > 0)
                {
                    RobotCategoryPropertiesSaveParameter parameter = new RobotCategoryPropertiesSaveParameter(ProductTypeId, CategoryId, CategoryName, saveValues);

                    RobotCategoryPropertiesSaveViewModel model = SizeableDialogDocumentManagerService.ShowView<RobotCategoryPropertiesSaveViewModel>(parameter, this);

                    if (model.IsOk)
                    {
                        CloseOk();
                    }
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нет изменений для сохранения");
            }

            return Task.CompletedTask;
        }

        private static (string value, string display) GetValue(int typeId, IChangeTracking value)
        {
            switch (typeId)
            {
                case RobotPropertyType.FlagId: return GetValueFromValueWrapper<bool?>(value);
                case RobotPropertyType.ListId: return GetValueFromValueWrapper<ComboBoxValue>(value);
                case RobotPropertyType.MultiListId: return GetValueFromValueWrapper<MultiComboBoxValue>(value);
                case RobotPropertyType.TextId: return GetValueFromValueWrapper<string>(value);
            }

            return (string.Empty, string.Empty);
        }

        private static (string value, string display) GetValueFromValueWrapper<T>(IChangeTracking changeTracking)
        {
            string result = string.Empty;
            string displayResult = string.Empty;

            if (changeTracking is ValidationTextValueWrapper textValueWrapper)
            {
                result = textValueWrapper.Value;
                displayResult = textValueWrapper.Value;
            }
            else if (changeTracking is ValidationFlagValueWrapper valueWrapper)
            {
                result = valueWrapper.Value.ToString();
                displayResult = valueWrapper.Value.ToString();
            }
            else if (changeTracking is ValidationMultiComboBoxValue value)
            {
                ComboBoxItem[] comboBoxItems = value.SelectedValues?.Cast<ComboBoxItem>().ToArray();

                string[] stringValues = comboBoxItems?.Length > 0 ? comboBoxItems.Select(x => x.Ref).ToArray() : Array.Empty<string>();
                string[] stringsDisplayValues = comboBoxItems?.Length > 0 ? comboBoxItems.Select(x => x.DisplayValue).ToArray() : Array.Empty<string>();

                result = JsonConvert.SerializeObject(stringValues);
                displayResult = JsonConvert.SerializeObject(stringsDisplayValues);
            }
            else if (changeTracking is ValidationComboBoxValue comboBoxValue)
            {
                result = comboBoxValue.SelectedValue?.Ref;
                displayResult = string.IsNullOrEmpty(comboBoxValue.SelectedValue?.DisplayValue)
                    ? string.Empty
                    : comboBoxValue.AvailValues.FirstOrDefault(x => x.Ref == comboBoxValue.SelectedValue?.Ref).DisplayValue;
            }

            return (result, displayResult);
        }

        private async Task LoadPropertiesAsync()
        {
            List<RobotPropertyDto> properties = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryRobotProperties()),
                "получении переменных робота",
                null,
                this,
                true,
                showNotification: false);

            _allProperties = properties;
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync(),
                "получении списка сотрудников",
                null,
                this,
                true,
                showNotification: false);

            Employees = employees.ToReadOnlyObservableCollection();
        }

        private async Task<List<RobotCategoryPropertyValueDto>> LoadCategoryPropertyValueAsync(int categoryId)
        {
            List<RobotCategoryPropertyValueDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryRobotCategoryPropertyValues(categoryId));

            if (dtos != null)
            {
                return dtos;
            }

            return Array.Empty<RobotCategoryPropertyValueDto>().ToList();
        }

        private IEnumerable<ValidationResultItem> CanSave(IReadOnlyCollection<RobotCategoryPropertyValueResult> saveValues)
        {
            if (saveValues?.Any(x => x.Item.Required && string.IsNullOrEmpty(x.ValueResult)) == true)
            {
                foreach (RobotCategoryPropertyValueResult item in saveValues.Where(x => x.Item.Required && string.IsNullOrEmpty(x.ValueResult)))
                {
                    yield return new ValidationResultItem($"Поле {item.Item.PropertyGroupName}: {item.Item.PropertyDisplayName} обязательно к заполнению", true);
                }
            }

            if (saveValues?.Any(x => !x.Item.Required && x.Item.ActiveNew && string.IsNullOrEmpty(x.ValueResult)) == true)
            {
                foreach (RobotCategoryPropertyValueResult item in saveValues.Where(x => !x.Item.Required && x.Item.ActiveNew && string.IsNullOrEmpty(x.ValueResult)))
                {
                    yield return new ValidationResultItem($"Поле {item.Item.PropertyGroupName}: {item.Item.PropertyDisplayName} активное, но не заполнено", true);
                }
            }

            if (saveValues?.Any(x => !string.IsNullOrEmpty(x.ValueResult) && !string.IsNullOrEmpty(x.Item.PropertyRegex) && !Regex.IsMatch(x.ValueResult, x.Item.PropertyRegex)) == true)
            {
                foreach (RobotCategoryPropertyValueResult item in saveValues.Where(x => !string.IsNullOrEmpty(x.ValueResult) && !string.IsNullOrEmpty(x.Item.PropertyRegex) && !Regex.IsMatch(x.ValueResult, x.Item.PropertyRegex)))
                {
                    yield return new ValidationResultItem($"{item.Item.PropertyDisplayName}: значение не соответсвует шаблону", true);
                }
            }
        }

        private PropertyGridRow[] GetRobotCategoryPropertyViewCollectionTypeDescriptor(RobotCategoryPropertyValueDto[] categoryPropertyValueDtos)
        {
            return _allProperties
                .Select(x => MapToPropertyGridRow(x, categoryPropertyValueDtos?.FirstOrDefault(y => y.PropertyId == x.Id)))
                .OrderBy(x => x.GroupPosition)
                .ThenBy(x => x.PropertyPosition)
                .ToArray();
        }

        private PropertyGridRow MapToPropertyGridRow(RobotPropertyDto propertyDto, RobotCategoryPropertyValueDto categoryPropertyDto)
        {
            IReadOnlyCollection<ComboBoxItem> robotPropertyValues = propertyDto.Values?.Count > 0
                ? propertyDto.Values.Select(x => new ComboBoxItem(x.Id, x.DisplayName, true, x.Value)).ToObservableCollection()
                : Array.Empty<ComboBoxItem>().ToObservableCollection();

            EmployeeDto employeeDto = Employees?.FirstOrDefault(x => x.Id == categoryPropertyDto?.ModifiedBy);

            RobotCategoryPropertyValueContentViewItem item = new RobotCategoryPropertyValueContentViewItem
            {
                CategoryId = categoryPropertyDto?.Id ?? 0,
                PropertyId = propertyDto.Id,
                ProductTypeId = categoryPropertyDto?.ProductTypeId ?? ProductTypeId,
                PropertyDisplayName = propertyDto.DisplayName,
                PropertyGroupName = propertyDto.GroupName,
                RobotPropertyType = Dictionaries.GetItemById<RobotPropertyType>(propertyDto.TypeId),
                PropertyRegex = propertyDto.Regex,
                PropertyDescription = propertyDto.Description,
                Position = propertyDto.Position,
                GroupPosition = propertyDto.GroupPosition,
                Active = propertyDto.Required || (categoryPropertyDto?.Active ?? propertyDto.Active),
                ActiveNew = propertyDto.Required || (categoryPropertyDto?.Active ?? propertyDto.Active),
                Required = propertyDto.Required,
                ModifiedOn = categoryPropertyDto?.ModifiedOn,
                ModifiedByName = employeeDto?.Name
            };

            switch (propertyDto.TypeId)
            {
                case RobotPropertyType.FlagId:

                    if (!string.IsNullOrEmpty(categoryPropertyDto?.Value))
                    {
                        bool.TryParse(categoryPropertyDto.Value, out bool val);

                        item.RobotCategoryValue = new ValidationFlagValueWrapper(val, item.Active);
                        item.RobotCategoryValueOld = new ValidationFlagValueWrapper(val, false, true);
                    }
                    else
                    {
                        item.RobotCategoryValue = new ValidationFlagValueWrapper(null, item.Active);
                        item.RobotCategoryValueOld = new ValidationFlagValueWrapper(null, false, true);
                    }

                    break;
                case RobotPropertyType.MultiListId:

                    ComboBoxItem[] values = Array.Empty<ComboBoxItem>();
                    ComboBoxItem[] values2 = Array.Empty<ComboBoxItem>();

                    if (!string.IsNullOrEmpty(categoryPropertyDto?.Value))
                    {
                        values = robotPropertyValues
                            .Where(x => JsonConvert.DeserializeObject<string[]>(categoryPropertyDto.Value).Contains(x.Ref))
                            .ToArray();

                        values2 = robotPropertyValues
                            .Where(x => JsonConvert.DeserializeObject<string[]>(categoryPropertyDto.Value).Contains(x.Ref))
                            .ToArray();
                    }

                    item.RobotCategoryValue = new ValidationMultiComboBoxValue(robotPropertyValues, values, active: item.Active);
                    item.RobotCategoryValueOld = new ValidationMultiComboBoxValue(robotPropertyValues, values2, isReadOnly: true);

                    break;
                case RobotPropertyType.ListId:

                    ComboBoxItem[] selected = robotPropertyValues.Where(x => x.Ref == categoryPropertyDto?.Value).ToArray();

                    item.RobotCategoryValue = new ValidationComboBoxValue(robotPropertyValues, selected.Length > 0 ? selected.First() : null, active: item.Active);
                    item.RobotCategoryValueOld = new ValidationComboBoxValue(robotPropertyValues, selected.Length > 0 ? selected.First() : null, isReadOnly: true);

                    break;
                default:

                    item.RobotCategoryValue = new ValidationTextValueWrapper(categoryPropertyDto?.Value, item.PropertyRegex, item.Active);
                    item.RobotCategoryValueOld = new ValidationTextValueWrapper(categoryPropertyDto?.Value, true);
                    break;
            }

            PropertyGridRow row = new PropertyGridRow(
                propertyDto.Name,
                propertyDto.DisplayName,
                propertyDto.GroupName,
                propertyDto.GroupPosition,
                propertyDto.Position,
                item,
                item.GetType(),
                !IsChangedColumnVisible);

            return row;
        }

        private RobotCategoryPropertyValueSaveViewItem MapToRobotCategoryPropertyValueSaveViewItem(RobotCategoryPropertyValueResult item)
        {
            return new RobotCategoryPropertyValueSaveViewItem(
                item.Item.CategoryId,
                item.Item.PropertyId,
                item.Item.PropertyDisplayName,
                item.Item.PropertyGroupName,
                item.OldValueResult,
                item.ValueResult,
                item.Item.Active,
                item.Item.ActiveNew,
                item.IsChanged || item.Item.Active != item.Item.ActiveNew,
                RobotCategoryPropertyValueSaveType.OnlyInCategory.Id)
            {
                CategoryPropertyValueDisplay = item.OldValueDisplayResult,
                CategoryPropertyValueDisplayNew = item.ValueDisplayResult
            };
        }

        private void OnProductTypeChanged()
        {
            IReadOnlyCollection<PropertyGridRow> rows = PropertyGridSource?.Rows;

            if (rows?.Any(x => x.Value is RobotCategoryPropertyValueContentViewItem item && (item.RobotCategoryValue?.IsChanged == true || item.Active != item.ActiveNew)) == true)
            {
                if (!MessageFacadeService.Confirm("Вы уверены что хотите изменить тип товара без сохранения изменений?"))
                {
                    ProductTypeId = _oldProductTypeId;
                    return;
                }
            }

            _oldProductTypeId = ProductTypeId;

            PropertyGridSource = new CollectionTypeDescriptor(GetRobotCategoryPropertyViewCollectionTypeDescriptor(_robotCategoryPropertyValueDtos.Where(x => x.ProductTypeId == ProductTypeId).ToArray()));
        }

        private struct RobotCategoryPropertyValueResult
        {
            public RobotCategoryPropertyValueContentViewItem Item { get; }

            public string OldValueResult { get; }

            public string OldValueDisplayResult { get; }

            public string ValueResult { get; }

            public string ValueDisplayResult { get; }

            public bool IsChanged { get; }

            public RobotCategoryPropertyValueResult(RobotCategoryPropertyValueContentViewItem item, (string Value, string ValueDisplay) valueOld, (string Value, string ValueDisplay) valueNew, bool isChanged)
            {
                Item = item;
                OldValueResult = valueOld.Value;
                OldValueDisplayResult = valueOld.ValueDisplay;
                ValueResult = valueNew.Value;
                ValueDisplayResult = valueNew.ValueDisplay;
                IsChanged = isChanged;
            }
        }
    }
}