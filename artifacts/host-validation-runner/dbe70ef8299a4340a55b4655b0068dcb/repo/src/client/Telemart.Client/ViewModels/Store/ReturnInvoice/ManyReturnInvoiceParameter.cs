using System;
using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class ManyReturnInvoiceParameter
    {
        public ManyReturnInvoiceParameter(
            int contractorId,
            int warehouseId,
            int? receiverCityId,
            DateTime returnDate,
            int carryId,
            int? ttnPayerTypeId,
            DeliveryDataDto deliveryData,
            List<InvoiceDto> invoiceDtos)
        {
            ContractorId = contractorId;
            WarehouseId = warehouseId;
            ReceiverCityId = receiverCityId;
            Invoices = invoiceDtos;
            ReturnDate = returnDate;
            CarryId = carryId;
            TtnPayerTypeId = ttnPayerTypeId;
            DeliveryData = deliveryData;
        }

        public int WarehouseId { get; }

        public int? ReceiverCityId { get; }

        public int ContractorId { get; }

        public DateTime ReturnDate { get; }

        public int? TtnPayerTypeId { get; }

        public int CarryId { get; }

        public DeliveryDataDto DeliveryData { get; }

        public List<InvoiceDto> Invoices { get; }
    }
}