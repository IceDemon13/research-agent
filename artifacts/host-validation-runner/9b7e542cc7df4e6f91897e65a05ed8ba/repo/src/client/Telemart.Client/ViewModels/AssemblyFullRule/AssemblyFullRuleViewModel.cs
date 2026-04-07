using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using DevExpress.XtraEditors.DXErrorProvider;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssemblyFullRule;
using Telemart.Client.Data.Requests.Features.AssemblyFullRule.Actions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyFullRule;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AssemblyFullRule
{
    public class AssemblyFullRuleViewModel : TelemartEditorViewModelBase<AssemblyFullRuleDto, AssemblyFullRuleParameter, AssemblyFullRuleViewItem>
    {
        public AssemblyFullRuleViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            FeatureValidateCommand = new DelegateCommand<ValidationEventArgs>(FeatureValidate);
        }

        public IDelegateCommand FeatureValidateCommand { get; }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<AssemblyFullRuleOperation> Operations
        {
            get { return GetProperty(() => Operations); }
            private set { SetProperty(() => Operations, value); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> FeatureValues
        {
            get { return GetProperty(() => FeatureValues); }
            private set { SetProperty(() => FeatureValues, value); }
        }

        protected override string CreatedActionMessage { get; } = "создано";

        protected override string EntityName { get; } = "Правило полноценности";

        protected override string UpdatedActionMessage { get; } = "сохранено";

        protected override async Task HandleLoadedAsync()
        {
            Operations = Dictionaries.GetItems<AssemblyFullRuleOperation>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            if (IsNew)
            {
                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                Categories = categories.Where(x => x.TypeId == CategoryType.PcComponents.Id && x.IsParent).OrderBy(x => x.Left).ToReadOnlyObservableCollection();
            }
        }

        protected override Task<Result<AssemblyFullRuleDto>> CreateEntityAsync()
        {
            AssemblyFullRuleSaveDto saveDto = new AssemblyFullRuleSaveDto
            {
                MasterCategoryId = Model.MasterCategoryId.Value,
                SlaveCategoryId = Model.SlaveCategoryId,
                OperationId = Model.Operation?.Id,
                FeatureId = Model.Feature?.Id,
                FeatureValueId = Model.FeatureValue?.Id,
                Active = Model.Active
            };

            return WebClient.ExecuteApiRequestAsync(new CreateAssemblyFullRule(saveDto));
        }

        protected override object CreateEntityMessage(AssemblyFullRuleDto dto, MessageType messageType)
        {
            return new AssemblyFullRuleMessage(dto, messageType);
        }

        protected override async Task<AssemblyFullRuleDto> GetEntityAsync(int id)
        {
            AssemblyFullRuleDto dto = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyFullRule(id));

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories.Where(x => (x.TypeId == CategoryType.PcComponents.Id && x.IsParent) || x.Id == dto.MasterCategoryId || x.Id == dto.SlaveCategoryId).OrderBy(x => x.Left).ToReadOnlyObservableCollection();

            if (dto.SlaveCategoryId.HasValue)
            {
                RefreshFeatures(dto.SlaveCategoryId.Value);
            }

            if (dto.Feature != null)
            {
                RefreshFeatureValues(dto.Feature.Id);
            }

            return dto;
        }

        protected override Task<LockResponse<AssemblyFullRuleDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAssemblyFullRule(id));
        }

        protected override Task<LockResponse<AssemblyFullRuleDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAssemblyFullRule(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание правила полноценности";
        }

        protected override void SetEditTitle()
        {
            Title = $"Правило полноценности №{Model.Id}";
        }

        protected override Task<Result<AssemblyFullRuleDto>> UpdateEntityAsync()
        {
            AssemblyFullRuleSaveDto saveDto = new AssemblyFullRuleSaveDto
            {
                Id = Model.Id,
                SlaveCategoryId = Model.SlaveCategoryId,
                OperationId = Model.Operation?.Id,
                FeatureId = Model.Feature?.Id,
                FeatureValueId = Model.FeatureValue?.Id,
                Active = Model.Active
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateAssemblyFullRule(saveDto));
        }

        protected override bool CanEdit()
        {
            return base.CanEdit() && WebClient.IsOperationAllowed(BusinessOperation.AssemblyFullRuleUpdate);
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AssemblyFullRuleViewItem.SlaveCategoryId):
                    Features = null;
                    Model.Feature = null;
                    Model.FeatureValue = null;

                    if (Model.SlaveCategoryId.HasValue)
                    {
                        RefreshFeatures(Model.SlaveCategoryId.Value);
                    }

                    break;
                case nameof(AssemblyFullRuleViewItem.Feature):
                    FeatureValues = null;
                    Model.FeatureValue = null;

                    if (Model.Feature.HasValue)
                    {
                        RefreshFeatureValues(Model.Feature.Value.Id);
                    }

                    break;
                case nameof(AssemblyFullRuleViewItem.Operation) when Model.Operation?.CanCompareValues != true:
                    Model.Feature = null;
                    Model.FeatureValue = null;
                    break;
            }
        }

        private static IEnumerable<HierarchicalItem> MapFeatureItems(IReadOnlyCollection<FeatureGroupDto> features)
        {
            foreach (FeatureGroupDto group in features.OrderBy(x => x.Position))
            {
                yield return new HierarchicalItem(group.Id, group.Name?.Trim());

                foreach (FeatureDto feature in group.Features.OrderBy(x => x.Position))
                {
                    yield return new HierarchicalItem(feature.Id, feature.Name?.Trim(), group.Id);
                }
            }
        }

        private void RefreshFeatures(int categoryId)
        {
            Features = MapFeatureItems(GetFeatures(categoryId)).ToReadOnlyObservableCollection();
        }

        private void RefreshFeatureValues(int featureId)
        {
            FeatureValues = GetFeatureValues(featureId).ToReadOnlyObservableCollection();
        }

        private IReadOnlyCollection<FeatureGroupDto> GetFeatures(int categoryId)
        {
            PagedResult<FeatureGroupDto> featureGroups = WebClient.ExecuteApiRequest(new QueryFeatureGroups(categoryId));

            if (!featureGroups.Data.Any(x => x.Features.Any()))
            {
                MessageFacadeService.ShowNotificationWarning("У выбранной категории нет характеристик");
                return Array.Empty<FeatureGroupDto>();
            }
            else
            {
                return featureGroups.Data;
            }
        }

        private IEnumerable<ComboBoxItem> GetFeatureValues(int featureId)
        {
            List<FeatureValueExDto> featureValues = WebClient.ExecuteApiRequest(new QueryFeatureValues(featureId));

            return featureValues
                .OrderBy(x => x.Value)
                .Select(x => new ComboBoxItem(x.Id, x.Value));
        }

        private void FeatureValidate(ValidationEventArgs e)
        {
            e.IsValid = e.Value == null || (e.Value is HierarchicalItem item && item.ParentId.HasValue);

            if (!e.IsValid)
            {
                e.ErrorType = ErrorType.Critical;
                e.ErrorContent = "Выберите характеристику, а не группу";
            }

            e.Handled = true;
        }
    }
}