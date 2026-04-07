using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerFilteringItem : PagingFilteringItem
    {
        public CustomerFilteringItem(string fio, int? contractorId, string phone, string email, string customerIds, string hashtagIds, int skip, int take)
        {
            Fio = fio;
            ContractorId = contractorId;
            Phone = phone;
            Email = email;
            CustomerIds = customerIds;
            HashtagIds = hashtagIds;
            Skip = skip;
            Take = take;
        }

        public CustomerFilteringItem(string phone, string email)
        {
            Phone = phone;
            Email = email;
        }

        public CustomerFilteringItem(string phone)
        {
            Phone = phone;
        }

        [FilteringItemProperty("fio")]
        public string Fio { get; }

        [FilteringItemProperty("contractor_id")]
        public int? ContractorId { get; }

        [FilteringItemProperty("phone")]
        public string Phone { get; }

        [FilteringItemProperty("email")]
        public string Email { get; }

        [FilteringItemProperty("customer_ids")]
        public string CustomerIds { get; }

        [FilteringItemProperty("hashtag_ids")]
        public string HashtagIds { get; }
    }
}
