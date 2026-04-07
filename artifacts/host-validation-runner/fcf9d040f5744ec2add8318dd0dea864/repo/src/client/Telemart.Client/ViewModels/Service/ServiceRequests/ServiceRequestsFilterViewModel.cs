using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class ServiceRequestsFilterViewModel : TelemartViewItemBase
    {
        private List<ContractorDto> contractorsList;

        public ServiceRequestsFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
            : this()
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        public ObservableRangeCollection<ComboBoxItem> Contractors { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ServiceRequestsFilterViewModel()
        {
            Reset();
        }

        public ObservableCollection<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public List<object> SelectedContractors
        {
            get { return GetProperty(() => SelectedContractors); }
            set { SetProperty(() => SelectedContractors, value); }
        }

        public DateTime? CreatedAfter
        {
            get { return GetProperty(() => CreatedAfter); }
            set { SetProperty(() => CreatedAfter, value); }
        }

        public DateTime? CreatedBefore
        {
            get { return GetProperty(() => CreatedBefore); }
            set { SetProperty(() => CreatedBefore, value); }
        }

        public DateTime? ChangedAfter
        {
            get { return GetProperty(() => ChangedAfter); }
            set { SetProperty(() => ChangedAfter, value); }
        }

        public DateTime? ChangedBefore
        {
            get { return GetProperty(() => ChangedBefore); }
            set { SetProperty(() => ChangedBefore, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public List<object> Managers
        {
            get { return GetProperty(() => Managers); }
            set { SetProperty(() => Managers, value); }
        }

        public int? GroupId
        {
            get { return GetProperty(() => GroupId); }
            set { SetProperty(() => GroupId, value); }
        }

        public string OrderNumbers
        {
            get { return GetProperty(() => OrderNumbers); }
            set { SetProperty(() => OrderNumbers, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
        }

        public string RequestNumbers
        {
            get { return GetProperty(() => RequestNumbers); }
            set { SetProperty(() => RequestNumbers, value); }
        }

        public string SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value); }
        }

        public bool? Bundle
        {
            get { return GetProperty(() => Bundle); }
            set { SetProperty(() => Bundle, value); }
        }

        public bool? CompletedOnFiscalRegistrar
        {
            get { return GetProperty(() => CompletedOnFiscalRegistrar); }
            set { SetProperty(() => CompletedOnFiscalRegistrar, value); }
        }

        public bool? CompletedOnMoneyRefund
        {
            get { return GetProperty(() => CompletedOnMoneyRefund); }
            set { SetProperty(() => CompletedOnMoneyRefund, value); }
        }

        public ObservableCollection<ServiceRequestState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public List<object> DiscussionStates
        {
            get { return GetProperty(() => DiscussionStates); }
            set { SetProperty(() => DiscussionStates, value); }
        }

        public List<object> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            set { SetProperty(() => Subdivisions, value); }
        }

        public List<object> RequirementTypes
        {
            get { return GetProperty(() => RequirementTypes); }
            set { SetProperty(() => RequirementTypes, value); }
        }

        public List<object> ResolutionTypes
        {
            get { return GetProperty(() => ResolutionTypes); }
            set { SetProperty(() => ResolutionTypes, value); }
        }

        public List<object> RejectReasons
        {
            get { return GetProperty(() => RejectReasons); }
            set { SetProperty(() => RejectReasons, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ServiceRequestsFilterViewModel> builder)
        {
            builder.Property(x => x.OrderNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
            builder.Property(x => x.RequestNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public Task RefreshAsync()
        {
            return Task.WhenAll(
                RefreshContractorsAsync());
        }

        public IFilteringItem GetFilteringItem()
        {
            return new ServiceRequestFilteringItem()
            {
                Cities = Cities?.Select(x => x.Id).ToArray(),
                Fio = Fio,
                Contractors = SelectedContractors?.Cast<ComboBoxItem>().Select(x => x.Id).ToArray(),
                TrackNumber = TrackNumber,
                CreatedAfter = CreatedAfter,
                CreatedBefore = CreatedBefore,
                Managers = Managers?.Cast<ComboBoxItem>().Select(x => x.Id).ToArray(),
                GroupId = GroupId,
                OrderNumbers = OrderNumbers,
                Product = Product,
                DiscussionStates = DiscussionStates?.Cast<int>().ToArray(),
                Email = Email,
                IncludeCustomerStateText = true,
                States = States?.Select(x => x.Id).ToArray(),
                Phone = Phone,
                SerialNumbers = SerialNumbers,
                Bundle = Bundle,
                ChangedAfter = ChangedAfter,
                ChangedBefore = ChangedBefore,
                RequestNumbers = RequestNumbers,
                RequirementTypes = RequirementTypes?.Cast<ServiceRequestRequirement>().Select(x => x.Id).ToArray(),
                RejectReasons = RejectReasons?.Select(x => (int)x).ToArray(),
                ResolutionTypes = ResolutionTypes?.Cast<ServiceRequestResolution>().Select(x => x.Id).ToArray(),
                Subdivisions = Subdivisions?.Cast<Subdivision>().Select(x => x.Id).ToArray(),
                CompletedOnFiscalRegistrar = CompletedOnFiscalRegistrar,
                CompletedOnMoneyRefund = CompletedOnMoneyRefund
            };
        }

        public void Reset()
        {
            Fio = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            RequestNumbers = string.Empty;
            OrderNumbers = string.Empty;
            CreatedAfter = null;
            CreatedBefore = null;
            ChangedAfter = null;
            ChangedBefore = null;
            Cities = null;
            SelectedContractors = null;
            States = new[] { ServiceRequestState.New, ServiceRequestState.Accepted, ServiceRequestState.InProgress, ServiceRequestState.Ready, ServiceRequestState.InRepair, ServiceRequestState.OnConfirmation }.ToObservableCollection();
            Product = string.Empty;
            SerialNumbers = null;
            Managers = null;
            Subdivisions = null;
            TrackNumber = string.Empty;
            RequirementTypes = null;
            ResolutionTypes = null;
            RejectReasons = null;
            Bundle = null;
            CompletedOnFiscalRegistrar = null;
            CompletedOnMoneyRefund = null;
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractors, contractorsList))
            {
                return;
            }

            Contractors.Clear();
            contractorsList = contractors;
            Contractors.AddRange(contractorsList.Where(x => x.Active && x.IsFolder == false).OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)));
        }
    }
}