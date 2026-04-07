using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderContractorViewItem : ValidatableItem
    {
        public Subdivision Subdivision { get; set; }

        public bool OldClient { get; set; }

        public int? EmployeeId { get; set; }

        public int? Buh1CId { get; set; }

        public int PriceTypeId { get; set; }

        public string Edrpou { get; set; }
    }
}