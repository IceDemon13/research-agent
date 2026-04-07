using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Area;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Area
{
    public sealed class AreasViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public AreasViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            EditCommand = new DelegateCommand(Edit, () => SelectedArea != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Messenger.Register<EntityMessage<AreaDto>>(this, OnAreaMessage);

            Areas = new ObservableRangeCollection<AreaViewItem>();
        }

        public AreasViewModel()
        {
        }

        #region Commands

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<AreaViewItem> Areas
        {
            get { return GetProperty(() => Areas); }
            set { SetProperty(() => Areas, value); }
        }

        public AreaViewItem SelectedArea
        {
            get { return GetProperty(() => SelectedArea); }
            set { SetProperty(() => SelectedArea, value); }
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

        private void Add()
        {
            DialogDocumentManagerService.ShowView<AreaViewModel>(null, this);
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<AreaViewModel>(new AreaParameter(SelectedArea.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<AreaDto> areas = await WebClient.ExecuteApiRequestAsync(new QueryAreas());

                Areas.Clear();

                Areas.AddRange(areas
                    .OrderBy(x => x.Name)
                    .Select(x => Mapper.Map<AreaViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnAreaMessage(EntityMessage<AreaDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    Areas.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}