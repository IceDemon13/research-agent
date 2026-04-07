using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.HotlineCompetitor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.HotlineCompetitor;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class HotlineCompetitorsViewModel : TelemartDialogViewModelBase
    {
        public HotlineCompetitorsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            EditHotlineCompetitorCommand = new DelegateCommand(EditHotlineCompetitor, () => SelectedHotlineCompetitor != null && CanEdit);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);

            messenger.Register<HotlineCompetitorMessage>(this, OnHotlineCompetitorMessage);
        }

        public IDelegateCommand EditHotlineCompetitorCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public ReadOnlyObservableCollection<HotlineCompetitorViewItem> HotlineCompetitors
        {
            get { return GetProperty(() => HotlineCompetitors); }
            private set { SetProperty(() => HotlineCompetitors, value); }
        }

        public HotlineCompetitorViewItem SelectedHotlineCompetitor
        {
            get { return GetProperty(() => SelectedHotlineCompetitor); }
            set { SetProperty(() => SelectedHotlineCompetitor, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<AbcType> AbcTypes
        {
            get { return GetProperty(() => AbcTypes); }
            private set { SetProperty(() => AbcTypes, value); }
        }

        public bool CanEdit => WebClient.IsOperationAllowed(BusinessOperation.HotlineCompetitorUpdate);

        #region DialogSettings

        public override int Height => 500;

        public override int MinHeight => 340;

        public override int MinWidth => 600;

        public override int Width => 800;

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            AbcTypes = Dictionaries.GetItems<AbcType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshHotlineCompetitors(), RefreshContractors());

            Title = "Конкуренты Hotline";

            async Task RefreshHotlineCompetitors()
            {
                List<HotlineCompetitorDto> hotlineCompetitors = await WebClient.ExecuteApiRequestAsync(new QueryHotlineCompetitors());

                HotlineCompetitors = hotlineCompetitors
                    .OrderBy(x => x.Name)
                    .Select(x => Mapper.Map<HotlineCompetitorViewItem>(x))
                    .ToReadOnlyObservableCollection();

                SelectedHotlineCompetitor = HotlineCompetitors.FirstOrDefault();
            }

            async Task RefreshContractors()
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                Contractors = contractors.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        private void EditHotlineCompetitor()
        {
            DialogDocumentManagerService.ShowView<HotlineCompetitorViewModel>(SelectedHotlineCompetitor, this);
        }

        private void OnHotlineCompetitorMessage(HotlineCompetitorMessage message)
        {
            if (message.MessageType == MessageType.Changed)
            {
                HotlineCompetitors.DoActionWithItem(x => x.Id == message.Entity.Id, item => Mapper.Map(message.Entity, item));
            }
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    EditHotlineCompetitorCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
