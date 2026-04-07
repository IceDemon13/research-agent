using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Content.ProductFeatureGroups.FeatureValues;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class ProductFeaturesCatalogViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ProductFeaturesCatalogViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddFeatureCommand = new DelegateCommand<BindableBase>(AddFeature, x => x != null);
            AddFeatureGroupCommand = new DelegateCommand(AddFeatureGroup, () => SelectedCategory != null);
            EditFeatureCommand = new DelegateCommand<BindableBase>(EditFeature, x => x != null);
            DeleteFeatureCommand = new AsyncCommand<BindableBase>(DeleteFeatureAsync, x => x != null);
            ReorderCommand = new DelegateCommand<BindableBase>(Reorder, x => x != null);
            EditMaskCommand = new DelegateCommand<int>(EditMask, x => SelectedCategory != null);
            ShowFeatureContractorParserSourceCommand = new DelegateCommand(ShowFeatureContractorParserSource, () => SelectedCategory != null);

            FeatureValuesCommand = new AsyncCommand<BindableBase>(FeatureValuesAsync, x => x is FeatureViewItem);

            Messenger.Register<FeatureMessage>(this, OnFeatureMessage);
            Messenger.Register<FeatureGroupMessage>(this, OnFeatureGroupMessage);
        }

        public ProductFeaturesCatalogViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand EditFeatureCommand { get; }

        public IDelegateCommand ShowFeatureContractorParserSourceCommand { get; }

        public IDelegateCommand AddFeatureCommand { get; }

        public IDelegateCommand AddFeatureGroupCommand { get; }

        public IAsyncCommand DeleteFeatureCommand { get; }

        public IDelegateCommand ReorderCommand { get; }

        public IDelegateCommand EditMaskCommand { get; }

        public IAsyncCommand FeatureValuesCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public CategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        public ObservableRangeCollection<FeatureGroupViewItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value); }
        }

        public BindableBase CurrentFeature
        {
            get { return GetProperty(() => CurrentFeature); }
            set { SetProperty(() => CurrentFeature, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            switch (msg.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;
                case HotkeyMessageType.Edit:
                    EditFeatureCommand.Execute(CurrentFeature);
                    handled = true;
                    break;
                case HotkeyMessageType.Delete:
                    DeleteFeatureCommand.Execute(CurrentFeature);
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Features = new ObservableRangeCollection<FeatureGroupViewItem>
            {
                new FeatureGroupViewItem
                {
                    Id = 1,
                    Name = "General",
                    CategoryId = 1,
                    Features = new ObservableCollection<FeatureViewItem> { new FeatureViewItem(), new FeatureViewItem() },
                    Position = 1
                },
                new FeatureGroupViewItem
                {
                    Id = 2,
                    Name = "Group1",
                    CategoryId = 1,
                    Features = new ObservableCollection<FeatureViewItem> { new FeatureViewItem() },
                    Position = 2
                },
                new FeatureGroupViewItem
                {
                    Id = 3,
                    Name = "Group2",
                    CategoryId = 1,
                    Position = 3
                }
            };
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();
            Categories = categories
                .Where(x => WebClient.AuthenticatedEmployee.AllowCategories.Contains(x.Id))
                .Select(x => Mapper.Map<CategoryViewItem>(x)).ToObservableCollection();
            RaisePropertyChanged(nameof(Categories));
        }

        private static IEnumerable<FeatureGroupViewItem> GetSortedFeatures(IReadOnlyCollection<FeatureGroupViewItem> featureGroups)
        {
            featureGroups.ForEach(x => x.Features = x.Features.OrderBy(y => y.Position).ToObservableCollection());

            return featureGroups.OrderBy(x => x.Position);
        }

        private async Task RefreshAsync()
        {
            if (SelectedCategory == null)
            {
                MessageFacadeService.ShowNotificationWarning("Категория не выбрана");
                return;
            }

            try
            {
                FeatureFilteringItem filteringItem = new FeatureFilteringItem(SelectedCategory.Id);

                Task<List<FeatureFullDto>> featureDtos = WebClient.ExecuteApiRequestAsync(new QueryFeatures(filteringItem)).GetPagedResultDataAsync();
                Task<List<FeatureGroupDto>> featureGroupDtos = WebClient.ExecuteApiRequestAsync(new QueryFeatureGroups(SelectedCategory.Id)).GetPagedResultDataAsync();

                await Task.WhenAll(featureDtos, featureGroupDtos);

                IReadOnlyDictionary<int, ObservableCollection<FeatureViewItem>> features = featureDtos.Result
                    .GroupBy(x => x.GroupId)
                    .ToDictionary(x => x.Key, y => Mapper.Map<ObservableCollection<FeatureViewItem>>(y.OrderBy(x => x.Position)));

                Features = featureGroupDtos.Result
                    .OrderBy(x => x.Position)
                    .Select(x => MapGroup(x, features.GetValueOrDefault(x.Id, new ObservableCollection<FeatureViewItem>()))).ToObservableRangeCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load features");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            FeatureGroupViewItem MapGroup(FeatureGroupDto group, ObservableCollection<FeatureViewItem> features)
            {
                FeatureGroupViewItem viewItem = Mapper.Map<FeatureGroupViewItem>(group);

                viewItem.Features = features;

                return viewItem;
            }
        }

        private Task DeleteFeatureAsync(BindableBase viewItem)
        {
            Task result;

            switch (viewItem)
            {
                case FeatureViewItem feature:
                    result = DeleteFeatureInternalAsync(feature);
                    break;
                case FeatureGroupViewItem featureGroup:
                    result = DeleteFeatureGroupInternalAsync(featureGroup);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return result;
        }

        private async Task DeleteFeatureInternalAsync(FeatureViewItem feature)
        {
            if (!MessageFacadeService.Confirm($"Вы уверены, что хотите удалить характеристику \"{feature.Name}\"?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteFeature(feature.Id));

                foreach (FeatureGroupViewItem featureGroupViewItem in Features)
                {
                    if (featureGroupViewItem.Features.Remove(feature))
                    {
                        break;
                    }
                }

                RaisePropertyChanged(nameof(Features));

                MessageFacadeService.ShowNotificationInfo($"Характеристика \"{feature.Name}\" успешно удалена");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении характеристики");
                ShowValidationResultView("Ошибки при удалении характеристики", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete feature");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении характеристики");
                Logger.LogError(exception, "Error while deleting feature");
            }
        }

        private async Task DeleteFeatureGroupInternalAsync(FeatureGroupViewItem featureGroup)
        {
            if (!MessageFacadeService.Confirm($"Вы уверены, что хотите удалить группу характеристик \"{featureGroup.Name}\"?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteFeatureGroup(featureGroup.Id));

                Features.Remove(featureGroup);

                RaisePropertyChanged(nameof(Features));

                MessageFacadeService.ShowNotificationInfo($"Группа характеристик \"{featureGroup.Name}\" успешно удалена");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении груупы характеристик");
                ShowValidationResultView("Ошибки при удалении группы характеристик", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete feature group");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении группы характеристик");
                Logger.LogError(exception, "Error while deleting feature group");
            }
        }

        private void ShowFeatureContractorParserSource()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.UpdateFeatureContractorParserSource))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            if (!SelectedCategory.IsParent)
            {
                MessageFacadeService.ShowNotificationError("Категория должна быть родительской");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<FeatureContractorParserSourcesViewModel>(
                new FeatureContractorParserSourcesParameter(SelectedCategory.Id, SelectedCategory.Name), this);
        }

        private void Reorder(BindableBase viewItem)
        {
            IReadOnlyCollection<ComboBoxItem> reorderItems;
            FeatureGroupViewItem featureParentGroup;

            switch (viewItem)
            {
                case FeatureViewItem feature:
                    featureParentGroup = Features.First(x => x.Id == feature.GroupId);

                    reorderItems = featureParentGroup.Features
                        .OrderBy(x => x.Position)
                        .Select(x => new ComboBoxItem(x.Id, x.Name))
                        .ToArray();

                    DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
                        new ReorderItemsParameter("Порядок", reorderItems, okCommand: x => SaveReorderedFeaturesAsync(x, featureParentGroup)),
                        this);

                    break;
                case FeatureGroupViewItem _:
                    reorderItems = Features
                        .OrderBy(x => x.Position)
                        .Select(x => new ComboBoxItem(x.Id, x.Name))
                        .ToArray();

                    DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
                        new ReorderItemsParameter("Порядок", reorderItems, okCommand: SaveReorderedFeatureGroupsAsync),
                        this);

                    break;
                default: throw new NotSupportedException();
            }
        }

        private async Task<bool> SaveReorderedFeaturesAsync(IReadOnlyCollection<ComboBoxItem> features, FeatureGroupViewItem featureParentGroup)
        {
            bool success = false;

            Dictionary<int, int> reorderedItems = features
                   .Select((x, i) => new { x.Id, Position = i })
                   .ToDictionary(x => x.Id, x => x.Position);

            try
            {
                IReadOnlyCollection<FeaturePositionDto> positions = reorderedItems.Select(x => new FeaturePositionDto(x.Key, x.Value)).ToList();

                await WebClient.ExecuteApiRequestAsync(new ReorderFeatures(featureParentGroup.Id, positions));

                foreach (FeatureViewItem feature in featureParentGroup.Features)
                {
                    if (reorderedItems.TryGetValue(feature.Id, out int position))
                    {
                        feature.Position = position;
                    }
                }

                Features = GetSortedFeatures(Features).ToObservableRangeCollection();

                MessageFacadeService.ShowNotificationInfo("Позиции характеристик успешно изменены");

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций характеристик");
                ShowValidationResultView("Ошибки при изменении позиций характеристик", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to reorder features");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций характеристик");
                Logger.LogError(exception, "Error while reordering features");
            }

            return success;
        }

        private async Task<bool> SaveReorderedFeatureGroupsAsync(IReadOnlyCollection<ComboBoxItem> groups)
        {
            bool success = false;

            Dictionary<int, int> reorderedItems = groups
                   .Select((x, i) => new { x.Id, Position = i })
                   .ToDictionary(x => x.Id, x => x.Position);

            try
            {
                IReadOnlyCollection<FeatureGroupPositionDto> positions = reorderedItems.Select(x => new FeatureGroupPositionDto(x.Key, x.Value)).ToList();

                await WebClient.ExecuteApiRequestAsync(new ReorderFeatureGroups(positions));

                foreach (FeatureGroupViewItem featureGroup in Features)
                {
                    if (reorderedItems.TryGetValue(featureGroup.Id, out int position))
                    {
                        featureGroup.Position = position;
                    }
                }

                Features = Features.OrderBy(x => x.Position).ToObservableRangeCollection();

                MessageFacadeService.ShowNotificationInfo("Позиции групп характеристик успешно изменены");

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций групп характеристик");
                ShowValidationResultView("Ошибки при изменении позиций групп характеристик", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to reorder feature groups");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций групп характеристик");
                Logger.LogError(exception, "Error while reordering feature groups");
            }

            return success;
        }

        private void AddFeature(BindableBase viewItem)
        {
            int featurePosition;
            int groupId;

            switch (viewItem)
            {
                case FeatureViewItem feature:
                    featurePosition = feature.Position;
                    groupId = feature.GroupId;
                    break;
                case FeatureGroupViewItem featureGroup:
                    featurePosition = 0;
                    groupId = featureGroup.Id;
                    break;
                default:
                    throw new NotSupportedException();
            }

            FeatureViewParameter parameter = new FeatureViewParameter(0, groupId, SelectedCategory.Id, featurePosition);
            DialogDocumentManagerService.ShowView<FeatureViewModel>(parameter, this);
        }

        private void AddFeatureGroup()
        {
            DialogDocumentManagerService.ShowView<FeatureGroupCreateViewModel>(SelectedCategory.Id, this);
        }

        private void EditFeature(BindableBase viewItem)
        {
            switch (viewItem)
            {
                case FeatureViewItem feature:
                    FeatureViewParameter param = new FeatureViewParameter(feature.Id, feature.GroupId, SelectedCategory.Id);
                    DialogDocumentManagerService.ShowView<FeatureViewModel>(param, this);
                    break;
                case FeatureGroupViewItem featureGroup:
                    FeatureGroupParameter parameter = new FeatureGroupParameter(featureGroup.Id, featureGroup.Name, featureGroup.NameUkr, featureGroup.NameEn);
                    DialogDocumentManagerService.ShowView<FeatureGroupViewModel>(parameter, this);
                    break;
            }
        }

        private void EditMask(int languageId)
        {
            if (SelectedCategory.IsParent)
            {
                SizeableDialogDocumentManagerService.ShowView<CategoryEditMaskViewModel>(new CategoryEditMaskParameter(SelectedCategory.Id, languageId), this);
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Маску разрешено править только для родительских категорий");
            }
        }

        private async Task FeatureValuesAsync(BindableBase item)
        {
            if (item is FeatureViewItem featureViewItem)
            {
                List<FeatureValueExDto> featureValues = await WebClient.ExecuteApiRequestAsync(new QueryFeatureValues(featureViewItem.Id));

                SizeableDialogDocumentManagerService.ShowView<FeatureValuesViewModel, FeatureValuesParameter, IReadOnlyCollection<FeatureValueViewItem>>(
                    new FeatureValuesParameter(SelectedCategory.Id, featureValues.Select(x => Mapper.Map<FeatureValueViewItem>(x)).ToArray(), featureViewItem.Id),
                    this);
            }
        }

        private void OnFeatureMessage(FeatureMessage message)
        {
            FeatureGroupViewItem featureGroup = Features.FirstOrDefault(x => x.Id == message.Entity.GroupId);

            if (featureGroup == null)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        FeatureViewItem oldFeature = Features.SelectMany(y => y.Features).FirstOrDefault(x => x.Id == message.Entity.Id);

                        if (oldFeature != null)
                        {
                            if (oldFeature.GroupId != message.Entity.GroupId)
                            {
                                FeatureGroupViewItem oldFeatureGroup = Features.FirstOrDefault(x => x.Id == oldFeature.GroupId);

                                oldFeatureGroup?.Features.Remove(oldFeature);

                                int position = message.Entity.Position > featureGroup.Features.Count || message.Entity.Position < 0 ? 0 : message.Entity.Position;
                                featureGroup.Features.Insert(position, Mapper.Map<FeatureViewItem>(message.Entity));
                            }
                            else
                            {
                                Mapper.Map(message.Entity, oldFeature);
                            }
                        }

                        break;
                    }

                case MessageType.Added:
                    {
                        FeatureViewItem viewItem = Mapper.Map<FeatureViewItem>(message.Entity);
                        int position = viewItem.Position > featureGroup.Features.Count || viewItem.Position < 0 ? 0 : viewItem.Position;
                        featureGroup.Features.Insert(position, viewItem);

                        break;
                    }
            }
        }

        private void OnFeatureGroupMessage(FeatureGroupMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        Features.DoActionWithItem(y => y.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                        break;
                    }

                case MessageType.Added:
                    {
                        FeatureGroupViewItem viewItem = Mapper.Map<FeatureGroupViewItem>(message.Entity);
                        Features.Add(viewItem);
                        break;
                    }
            }
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);
        }
    }
}