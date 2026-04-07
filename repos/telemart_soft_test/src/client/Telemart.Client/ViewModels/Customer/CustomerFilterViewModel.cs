using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Hashtag;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerFilterViewModel : TelemartViewItemBase
    {
        public CustomerFilterViewModel(IWebClient webClient)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }

        public CustomerFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Contractors { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> Hashtags { get; } = new ObservableRangeCollection<ComboBoxItem>();

        #endregion

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
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

        public string CustomerIds
        {
            get { return GetProperty(() => CustomerIds); }
            set { SetProperty(() => CustomerIds, value); }
        }

        public List<object> SelectedHashtags
        {
            get { return GetProperty(() => SelectedHashtags); }
            set { SetProperty(() => SelectedHashtags, value); }
        }

        private IWebClient WebClient { get; }

        public static void BuildMetadata(MetadataBuilder<CustomerFilterViewModel> builder)
        {
            builder.Property(x => x.CustomerIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public CustomerFilteringItem GetFilteringItem(int skip, int take)
        {
            IEnumerable<int> hashtagIds = SelectedHashtags?.Cast<ComboBoxItem>().Select(x => x.Id);

            CustomerFilteringItem item = new CustomerFilteringItem(
                Fio,
                ContractorId,
                Phone,
                Email,
                CustomerIds,
                hashtagIds is null ? null : string.Join(",", hashtagIds),
                skip,
                take);

            return item;
        }

        public Task RefreshAsync()
        {
            return Task.WhenAll(RefreshContractorsAsync(), RefreshHashtagsAsync());
        }

        public void ResetFilterValues()
        {
            Fio = null;
            ContractorId = null;
            Phone = null;
            Email = null;
            CustomerIds = null;
            SelectedHashtags = null;
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Contractors.Clear();

            List<ComboBoxItem> contractorItems = contractors
                .Where(x => x.Active && x.IsClient && !x.IsFolder)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Contractors.AddRange(contractorItems);
        }

        private async Task RefreshHashtagsAsync()
        {
            List<HashtagDto> hashtags = await WebClient.ExecuteApiRequestAsync(new QueryHashtags(), true);

            Hashtags.Clear();

            List<ComboBoxItem> hashtagItems = hashtags
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Hashtags.AddRange(hashtagItems);
        }
    }
}