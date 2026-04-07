using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderGiveCellParameter
    {
        public OrderGiveCellParameter(OrderDto order, List<OrderCellDto> orderCells, string cityName, string contractorName, string title, bool printDocuments, int? billId, bool printActOutcomeGuestProduct)
        {
            Order = order;
            CityName = cityName;
            ContractorName = contractorName;
            Title = title;
            PrintDocuments = printDocuments;
            OrderCells = orderCells;
            BillId = billId;
            PrintActOutcomeGuestProduct = printActOutcomeGuestProduct;
        }

        public OrderDto Order { get; }

        public string CityName { get; }

        public string ContractorName { get; }

        public string Title { get; }

        public bool PrintDocuments { get; }

        public bool PrintActOutcomeGuestProduct { get; }

        public int? BillId { get; }

        public List<OrderCellDto> OrderCells { get; }
    }
}