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
using Telemart.Client.Data.Requests.Features.ProductCompatibility;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ProductCompatibility;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.ProductCompatibility
{
    public class ProductCompatibilitiesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private List<CategoryDto> categoriesList;

        public ProductCompatibilitiesViewModel(
               IWebClient webClient,
               IDictionaries dictionaries,
               IMessageFacadeService messageFacadeService,
               IMapper mapper,
               IMessenger messenger)
               : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            CreateCommand = new DelegateCommand<ProductCompatibilityType>(Create);
            EditCommand = new DelegateCommand(Edit, () => SelectedProductCompatibility != null);
            DeleteCommand = new AsyncCommand(DeleteAsync, () => SelectedProductCompatibility != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Messenger.Register<AssemblyCompatibilityMessage>(this, OnAssemblyCompatibilityMessage);

            ProductCompatibilities = new ObservableRangeCollection<ProductCompatibilityViewItem>();

            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.AssemblyCompatibilityCreate);
            CanEdit = WebClient.IsOperationAllowed(BusinessOperation.AssemblyCompatibilityUpdate);
            CanDelete = WebClient.IsOperationAllowed(BusinessOperation.AssemblyCompatibilityDelete);
        }

        public ProductCompatibilitiesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CreateCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand DeleteCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<NotificationImage> NotificationImages
        {
            get { return GetProperty(() => NotificationImages); }
            private set { SetProperty(() => NotificationImages, value); }
        }

        public ReadOnlyObservableCollection<ProductCompatibilityType> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ObservableRangeCollection<ProductCompatibilityViewItem> ProductCompatibilities
        {
            get { return GetProperty(() => ProductCompatibilities); }
            private set { SetProperty(() => ProductCompatibilities, value); }
        }

        public ProductCompatibilityViewItem SelectedProductCompatibility
        {
            get { return GetProperty(() => SelectedProductCompatibility); }
            set { SetProperty(() => SelectedProductCompatibility, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool CanCreate { get; private set; }

        public bool CanEdit { get; private set; }

        public bool CanDelete { get; private set; }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

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
                    if (EditCommand.CanExecute(null))
                    {
                        EditCommand.Execute(null);
                    }

                    handled = true;
                    break;

                case HotkeyMessageType.Delete:
                    if (DeleteCommand.CanExecute(null))
                    {
                        DeleteCommand.Execute(null);
                    }

                    handled = true;
                    break;

                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override void OnInitializeInDesignMode()
        {
            CanCreate = true;
            CanEdit = true;
            CanDelete = true;
        }

        protected override Task HandleLoadedAsync()
        {
            Types = Dictionaries.GetItems<ProductCompatibilityType>().ToReadOnlyObservableCollection();

            NotificationImages = Dictionaries.GetItems<NotificationImage>().ToReadOnlyObservableCollection();

            RefreshCommand.Execute(null);

            return Task.CompletedTask;
        }

        private void Create(ProductCompatibilityType type)
        {
            DialogDocumentManagerService.ShowView<ProductCompatibilityViewModel>(new ProductCompatibilityParameter(0, type.Id), this);
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<ProductCompatibilityViewModel>(new ProductCompatibilityParameter(SelectedProductCompatibility.Id), this);
        }

        private async Task DeleteAsync()
        {
            const string ErrorMessage = "Ошибка при удалении правила";

            if (!MessageFacadeService.Confirm("Вы действительно хотите удалить правило?"))
            {
                return;
            }

            try
            {
                Result<object> result = await WebClient.ExecuteApiRequestAsync(new DeleteProductCompatibility(SelectedProductCompatibility.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Правило удалено с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Правило успешно удалено");
                }

                ProductCompatibilities.Remove(SelectedProductCompatibility);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorMessage);
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete rules");
                MessageFacadeService.ShowNotificationError(ErrorMessage);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete rules");
                MessageFacadeService.ShowNotificationError(ErrorMessage);
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                await RefreshCategories();

                List<ProductCompatibilityDto> assemblyCompatibilities = await WebClient.ExecuteApiRequestAsync(new QueryProductCompatibilities());

                ProductCompatibilities.Clear();

                ProductCompatibilities.AddRange(assemblyCompatibilities
                    .OrderBy(x => x.Id)
                    .Select(x => Mapper.Map<ProductCompatibilityViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            async Task RefreshCategories()
            {
                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                if (ReferenceEquals(categories, categoriesList))
                {
                    return;
                }

                categoriesList = categories;

                Categories = categoriesList
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        private void OnAssemblyCompatibilityMessage(AssemblyCompatibilityMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    ProductCompatibilities.Insert(0, Mapper.Map<ProductCompatibilityViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    ProductCompatibilities.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private bool ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }
    }
}