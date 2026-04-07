using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.TradeInSegment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeInSegment;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public sealed class TradeInSegmentViewModel : TelemartEditorViewModelBase<TradeInSegmentDto, TradeInSegmentParameter, TradeInSegmentViewItem>
    {
        public TradeInSegmentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            AddFeatureValueCommand = new DelegateCommand(AddFeatureValue, () => SelectedCategoryFeature != null);
            DeleteAllFeatureValuesCommand = new DelegateCommand(DeleteAllFeatureValues, () => SelectedCategoryFeature?.FeatureValues?.Any() == true);
        }

        public TradeInSegmentViewModel()
        {
        }

        public IDelegateCommand AddFeatureValueCommand { get; }

        public IDelegateCommand DeleteAllFeatureValuesCommand { get; }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<TradeInSegmentCategorySettingsDto> CategorySettings
        {
            get { return GetProperty(() => CategorySettings); }
            set { SetProperty(() => CategorySettings, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public TradeInSegmentCategoryFeatureViewItem SelectedCategoryFeature
        {
            get { return GetProperty(() => SelectedCategoryFeature); }
            set { SetProperty(() => SelectedCategoryFeature, value); }
        }

        public bool AllowEditEmployee
        {
            get { return GetProperty(() => AllowEditEmployee); }
            set { SetProperty(() => AllowEditEmployee, value); }
        }

        #region DialogSettings

        public override int Width => 900;

        public override int MinWidth => 700;

        public override int MaxWidth => 1400;

        public override int Height => 600;

        public override int MinHeight => 400;

        public override int MaxHeight => 800;

        #endregion

        protected override string CreatedActionMessage => "создан";

        protected override string EntityName => "Trade-In сегмент";

        protected override string UpdatedActionMessage => "соxранен";

        protected override Task<Result<TradeInSegmentDto>> CreateEntityAsync()
        {
            Model.CategoryFeatures.ForEach(x => x.DisplayFeatureValues ??= new List<object>());

            TradeInSegmentCreateDto dto = new TradeInSegmentCreateDto(
                Model.Name,
                Model.Price,
                Model.CategoryId!.Value,
                Model.EmployeeId!.Value,
                Model.CategoryFeatures
                    .SelectMany(x => x.DisplayFeatureValues)
                    .Cast<TradeInSegmentCategoryFeatureValueViewItem>()
                    .Select(x => new TradeInSegmentCategoryFeatureSimpleDto(x.SegmentCategorySettingsId, x.FeatureValueId))
                    .ToArray());

            CreateTradeInSegment gatewayRequest = new CreateTradeInSegment(dto);

            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<TradeInSegmentDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryTradeInSegment(id));
        }

        protected override Task<LockResponse<TradeInSegmentDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<TradeInSegmentDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChangedInternal(sender, e);

            switch (e.PropertyName)
            {
                case nameof(TradeInSegmentViewItem.CategoryId):

                    if (Model.CategoryId.HasValue)
                    {
                        if (Model.EmployeeId is null)
                        {
                            Model.EmployeeId = Categories.First(x => x.Id == Model.CategoryId).EmployeeId;
                        }

                        if (IsNew)
                        {
                            Model.CategoryFeatures = CategorySettings
                                .Where(x => x.CategoryId == Model.CategoryId.Value)
                                .Select(x => new TradeInSegmentCategoryFeatureViewItem(0, Model.Id, x.Id, x.CategoryId, x.FeatureId, x.FeatureName, new ObservableCollection<TradeInSegmentCategoryFeatureValueViewItem>()))
                                .ToObservableCollection();
                        }
                    }

                    break;
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            List<TradeInSegmentCategorySettingsDto> categorySettings = await WebClient.ExecuteApiRequestAsync(new QueryTradeInSegmentCategorySettings());

            CategorySettings = categorySettings.ToReadOnlyObservableCollection();

            HashSet<int> categoryIds = categorySettings.GroupBy(x => x.CategoryId).Select(x => x.Key).ToHashSet();

            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            Categories = categories.Data
                .Where(x => categoryIds.Contains(x.Id))
                .ToReadOnlyObservableCollection();

            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees());

            Employees = employees.Data
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            AllowEditEmployee = WebClient.IsOperationAllowed(BusinessOperation.AllowEditTradeInSegmentEmployee);

            await base.HandleLoadedAsync();

            if (!IsNew)
            {
                foreach (TradeInSegmentCategorySettingsDto setting in CategorySettings.Where(x => x.CategoryId == Model.CategoryId!.Value && !Model.CategoryFeatures.Select(q => q.FeatureId).Contains(x.FeatureId)))
                {
                    Model.CategoryFeatures.Add(new TradeInSegmentCategoryFeatureViewItem(
                        0,
                        Model.Id,
                        setting.Id,
                        setting.CategoryId,
                        setting.FeatureId,
                        setting.FeatureName,
                        new ObservableCollection<TradeInSegmentCategoryFeatureValueViewItem>()));
                }
            }
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание Trade-In сегмента";
        }

        protected override void SetEditTitle()
        {
            Title = $"Trade-In сегмент ({Model.Id})";
        }

        protected override bool IsValid(TradeInSegmentViewItem model)
        {
            if (Model.CategoryFeatures.Any(x => x.DisplayFeatureValues?.Any() != true))
            {
                MessageFacadeService.ShowMessageBoxError("Для каждой характеристики в Trade-In сегменте должно быть задано минимум одно значение характеристик");
                return false;
            }

            return true;
        }

        protected override Task<Result<TradeInSegmentDto>> UpdateEntityAsync()
        {
            Model.CategoryFeatures.ForEach(x => x.DisplayFeatureValues ??= new List<object>());

            TradeInSegmentUpdateDto dto = new TradeInSegmentUpdateDto(
                Model.Id,
                Model.Name,
                Model.Price,
                Model.EmployeeId!.Value,
                Model.CategoryFeatures
                    .SelectMany(x => x.DisplayFeatureValues)
                    .Cast<TradeInSegmentCategoryFeatureValueViewItem>()
                    .Select(x => new TradeInSegmentCategoryFeatureSimpleDto(x.SegmentCategorySettingsId, x.FeatureValueId))
                    .ToArray());

            UpdateTradeInSegment gatewayRequest = new UpdateTradeInSegment(dto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        private void DeleteAllFeatureValues()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            SelectedCategoryFeature.FeatureValues.Clear();
            SelectedCategoryFeature.DisplayFeatureValues.Clear();
        }

        private void AddFeatureValue()
        {
            GetCategoryFeatureValueViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureValueViewModel>(
                new GetCategoryFeatureValueParameter(
                true,
                false,
                SelectedCategoryFeature.CategoryId,
                featureId: SelectedCategoryFeature.FeatureId,
                featureName: SelectedCategoryFeature.FeatureName,
                multiSelectFeatureValue: true,
                excludedFeatureValueIds: SelectedCategoryFeature.DisplayFeatureValues?.Cast<TradeInSegmentCategoryFeatureValueViewItem>().Select(x => x.FeatureValueId).ToArray()),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            List<ComboBoxItem> selectedFeatureValues = viewModel.GetSelectedFeatureValues();

            if (SelectedCategoryFeature.DisplayFeatureValues?
                    .Cast<TradeInSegmentCategoryFeatureValueViewItem>()
                    .Select(x => x.FeatureValueId)
                    .Any(x => selectedFeatureValues.Any(z => z.Id == x)) == true)
            {
                MessageFacadeService.ShowNotificationWarning("Такое значение характеристики уже добавлено");
                return;
            }

            SelectedCategoryFeature.FeatureValues ??= new ObservableCollection<TradeInSegmentCategoryFeatureValueViewItem>();
            SelectedCategoryFeature.DisplayFeatureValues ??= new List<object>();

            foreach (ComboBoxItem featureValueItem in selectedFeatureValues)
            {
                TradeInSegmentCategoryFeatureValueViewItem featureValue = new TradeInSegmentCategoryFeatureValueViewItem(featureValueItem.Id, featureValueItem.DisplayValue, SelectedCategoryFeature.TradeInSegmentCategorySettingsId);

                SelectedCategoryFeature.FeatureValues.Add(featureValue);

                SelectedCategoryFeature.DisplayFeatureValues.Add(featureValue);

                SelectedCategoryFeature.DisplayFeatureValues = SelectedCategoryFeature.DisplayFeatureValues
                    .Cast<TradeInSegmentCategoryFeatureValueViewItem>()
                    .OrderBy(x => x.FeatureValueName)
                    .Cast<object>()
                    .ToList();

                SelectedCategoryFeature.DisplayFeatureValues = new List<object>(SelectedCategoryFeature.DisplayFeatureValues);
            }

            RaisePropertiesChanged(nameof(TradeInSegmentCategoryFeatureViewItem.DisplayFeatureValues), nameof(TradeInSegmentCategoryFeatureViewItem.FeatureValues));
        }
    }
}