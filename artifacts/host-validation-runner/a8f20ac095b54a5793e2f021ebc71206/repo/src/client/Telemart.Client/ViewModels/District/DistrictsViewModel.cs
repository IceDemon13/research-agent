using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.District;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.District
{
    public sealed class DistrictsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IMessenger messenger;

        private readonly IMapper mapper;

        public DistrictsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
        : base(webClient, dictionaries, messageFacadeService)
        {
            this.messenger = messenger ?? throw new ArgumentException(nameof(messenger));
            this.mapper = mapper ?? throw new ArgumentException(nameof(mapper));

            this.messenger.Register<EntityMessage<DistrictDto>>(this, OnDistrictMessage);

            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedDistrict != null);

            Districts = new ObservableRangeCollection<DistrictViewItem>();
        }

        #region Commands

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<DistrictViewItem> Districts
        {
            get { return GetProperty(() => Districts); }
            set { SetProperty(() => Districts, value); }
        }

        public DistrictViewItem SelectedDistrict
        {
            get { return GetProperty(() => SelectedDistrict); }
            set { SetProperty(() => SelectedDistrict, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>(nameof(DialogDocumentManagerService), ServiceSearchMode.PreferParents);

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
                case HotkeyMessageType.Add:
                    AddCommand.Execute(null);
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

        private async Task RefreshAsync()
        {
            try
            {
                Districts.Clear();
                List<DistrictDto> districtDtos = await WebClient.ExecuteApiRequestAsync(new QueryDistricts());
                Districts.AddRange(districtDtos.OrderBy(x => x.Name).Select(x => mapper.Map<DistrictViewItem>(x)));
            }
            catch (Exception exception)
            {
                string str = exception.ToString();
                Console.WriteLine(str);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<DistrictViewModel>(new DistrictParameter(SelectedDistrict.Id), this);
        }

        private void Add()
        {
            DialogDocumentManagerService.ShowView<DistrictViewModel>(new DistrictParameter(0), this);
        }

        private void OnDistrictMessage(EntityMessage<DistrictDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    Districts.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => mapper.Map(message.Entity, viewItem));
                    break;
                case MessageType.Added:
                    Districts.Add(mapper.Map<DistrictViewItem>(message.Entity));
                    break;
            }
        }
    }
}