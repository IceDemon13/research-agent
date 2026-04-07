using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Contractor;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderCopyEditContractorViewModel : TelemartDialogViewModelBase
    {
        private OrderEditContractorParameter data;

        public OrderCopyEditContractorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
        }

        #region INPC

        public ContractorViewItem CurrentContractor
        {
            get { return GetProperty(() => CurrentContractor); }
            set { SetProperty(() => CurrentContractor, value); }
        }

        public ContractorViewItem SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value); }
        }

        public ObservableCollection<ContractorViewItem> AllContractors
        {
            get { return GetProperty(() => AllContractors); }
            private set { SetProperty(() => AllContractors, value); }
        }

        public ObservableCollection<ContractorViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        public static void BuildMetadata(MetadataBuilder<OrderEditContractorViewModel> builder)
        {
            builder.Property(x => x.SelectedContractor).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            data = (OrderEditContractorParameter)Parameter;

            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            AllContractors = contractors.Select(x => Mapper.Map<ContractorViewItem>(x)).ToObservableCollection();

            SelectedContractor = CurrentContractor = AllContractors.FirstOrDefault(x => x.Id == data.ClientId);

            Contractors = AllContractors
                .Where(x => !x.IsFolder
                            && x.IsClient
                            && x.Active
                            && WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.Subdivision.Id))
                .OrderBy(x => x.Subdivision.Id)
                .ThenBy(x => x.Name)
                .ToObservableCollection();

            Title = "Контрагент";
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this, 1))
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}
