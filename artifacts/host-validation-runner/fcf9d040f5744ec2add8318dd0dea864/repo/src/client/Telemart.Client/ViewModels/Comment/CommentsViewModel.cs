using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Comment;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Comment;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Comment
{
    public sealed class CommentsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public CommentsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            AddCommand = new DelegateCommand(Add);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedComment != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            ChangeCommentTypeCommand = new AsyncCommand(ChangeCommentTypeAsync);
            ValidateCommentsCommand = new AsyncCommand(ValidateCommentsAsync, () => ValidateCommentsVisible);

            Filter = new CommentFilterViewModel(webClient, dictionaries, mapper);

            Messenger.Register<CommentMessage>(this, OnCommentTaskMessage);
        }

        public CommentsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand ValidateCommentsCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IAsyncCommand ChangeCommentTypeCommand { get; }

        #endregion

        #region INPC

        public CommentFilterViewModel Filter { get; }

        public ReadOnlyObservableCollection<ComboBoxItem> Entities
        {
            get { return GetProperty(() => Entities); }
            private set { SetProperty(() => Entities, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ObservableRangeCollection<CommentViewItem> Comments
        {
            get { return GetProperty(() => Comments); }
            set { SetProperty(() => Comments, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public CommentViewItem SelectedComment
        {
            get { return GetProperty(() => SelectedComment); }
            set { SetProperty(() => SelectedComment, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool ValidateCommentsVisible => WebClient.IsOperationAllowed(BusinessOperation.CommentsValidation);

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else if (msg.ModifierKeys == ModifierKeys.None && msg.Key == Key.F9)
            {
                ChangeCommentTypeCommand.Execute(null);
                handled = true;
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        break;
                    case HotkeyMessageType.Add:
                        AddCommand.Execute(null);
                        break;
                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(null);
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            Comments = Comments ?? new ObservableRangeCollection<CommentViewItem>();

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();
            Categories = categories.Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableRangeCollection();

            Entities = Dictionaries
                .GetItems<Entity>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            IsSearchPanelClosed = false;
            await Filter.RefreshAsync();
            CancelFilteringCommand.Execute(null);
        }

        private static CommentType GetNewCommentType(CommentType oldType)
        {
            if (oldType == null)
            {
                return null;
            }

            return oldType == CommentType.Comment
                ? CommentType.Question
                : CommentType.Comment;
        }

        private static void Add()
        {
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            SizeableDialogDocumentManagerService.ShowView<CommentAnswersViewModel>(new CommentAnswersParameter(SelectedComment.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                Comments.Clear();

                await Filter.RefreshAsync();

                PagedResult<CommentSimpleDto> comments = await WebClient.ExecuteApiRequestAsync(new QueryComments(Filter.GetCommentFilteringItem()));

                Comments.AddRange(comments.Data.Select(x => Mapper.Map<CommentViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnCommentTaskMessage(CommentMessage message)
        {
            if (message.Entity.ParentId.HasValue)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Added:
                    Comments.Insert(0, Mapper.Map<CommentViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Comments.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                Filter.Product = product.Name;
            }
        }

        private async Task ValidateCommentsAsync()
        {
            string commentValidationStr = await WebClient.ExecuteApiRequestAsync(new QueryCommentValidation());

            bool commentValidation = Convert.ToBoolean(commentValidationStr);

            IReadOnlyCollection<YesNo> yesNoValues = Dictionaries.GetItems<YesNo>();

            ChangeItemViewModel viewModel = DialogDocumentManagerService.ShowView<ChangeItemViewModel>(
                new ChangeItemParameter(
                    yesNoValues.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                    yesNoValues.First(x => x.Boolean == commentValidation).Name,
                    "Модерация"),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            await WebClient.ExecuteApiRequestAsync(new UpdateCommentValidation(new UpdateCommentValidationDto(yesNoValues.First(x => x.Id == viewModel.NewItem!.Value.Id).Boolean)));

            MessageFacadeService.ShowNotificationInfo("Настройки модерации изменены успешно");
        }

        private async Task ChangeCommentTypeAsync()
        {
            CommentType newType = GetNewCommentType(SelectedComment.Type);

            if (!MessageFacadeService.Confirm($"Вы уверены, что хотитите изменить тип отзыва на \"{newType.Name}\""))
            {
                return;
            }

            try
            {
                Result<CommentFullDto> result = await WebClient.ExecuteApiRequestAsync(new ChangeTypeComment(SelectedComment.Id, newType.Id));

                Messenger.Send(new CommentMessage(result.Data, MessageType.Changed));

                MessageFacadeService.ShowNotificationInfo($"Тип комментария изменен на \"{newType.Name}\"");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении типа");
                ShowValidationResultView("Ошибка при изменении типа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to change comment type");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении типа");
                Logger.LogError(exception, "Error while changing comment type");
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