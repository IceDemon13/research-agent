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
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Carry.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Carry
{
    public sealed class CarriesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public CarriesViewModel(
               IWebClient webClient,
               IDictionaries dictionaries,
               IMessageFacadeService messageFacadeService,
               IMapper mapper,
               IMessenger messenger)
               : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            EditCommand = new DelegateCommand(Edit, () => SelectedCarry != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            ReorderCommand = new DelegateCommand(Reorder);

            Messenger.Register<CarryMessage>(this, OnCarryMessage);

            Carries = new ObservableRangeCollection<CarryViewItem>();
        }

        public CarriesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ReorderCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<CarryViewItem> Carries
        {
            get { return GetProperty(() => Carries); }
            set { SetProperty(() => Carries, value); }
        }

        public CarryViewItem SelectedCarry
        {
            get { return GetProperty(() => SelectedCarry); }
            set { SetProperty(() => SelectedCarry, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Types
        {
            get { return GetProperty(() => Types); }
            set { SetProperty(() => Types, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

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
                    EditCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            return RefreshAsync();
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<CarryViewModel>(new CarryParameter(SelectedCarry.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<CarryDto> carries = await WebClient.ExecuteApiRequestAsync(new QueryCarries());
                List<CarryTypeDto> types = await WebClient.ExecuteApiRequestAsync(new QueryCarryTypes());

                Carries.Clear();

                Carries.AddRange(carries
                    .OrderByDescending(x => x.CarryTypeId)
                    .ThenBy(x => x.Position)
                    .Select(x => Mapper.Map<CarryViewItem>(x)));

                Types = types.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnCarryMessage(CarryMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    Carries.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private void Reorder()
        {
            IEnumerable<ComboBoxItem> carries = Carries.OrderBy(x => x.Position).Select(x => new ComboBoxItem(x.Id, x.Name));

            ReorderItemsViewModel viewModel = DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
             new ReorderItemsParameter("Порядок способов доставки", carries, okCommand: ReorderHandleOkAsync),
             this);
        }

        private async Task<bool> ReorderHandleOkAsync(IReadOnlyCollection<ComboBoxItem> items)
        {
            CarryPositionDto[] carryPositions = items.Select((x, i) => new CarryPositionDto(x.Id, i)).ToArray();

            try
            {
                Result<object> result = await WebClient.ExecuteApiRequestAsync(new ReorderCarries(carryPositions));

                Dictionary<int, int> positions = items.Select((x, i) => new { x.Id, i }).ToDictionary(x => x.Id, x => x.i);

                foreach (CarryViewItem x in Carries)
                {
                    x.Position = positions[x.Id];
                }

                List<CarryViewItem> carries = Carries.OrderByDescending(x => x.CarryTypeId).ThenBy(x => x.Position).ToList();
                Carries.ReplaceRange(carries);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Изменения сохранены с предупреждениями");
                    MessageFacadeService.ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList(), this);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Изменения успешно сохранены");
                }

                return true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to order carries");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении изменений");
                return false;
            }
        }
    }
}