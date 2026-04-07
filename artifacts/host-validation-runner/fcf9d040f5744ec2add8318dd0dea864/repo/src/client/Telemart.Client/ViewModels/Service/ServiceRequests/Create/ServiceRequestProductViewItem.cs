using System.Collections.Generic;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public class ServiceRequestProductViewItem : TelemartViewItemBase
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public OrderPromoCodeDto OrderPromoCode
        {
            get { return GetProperty(() => OrderPromoCode); }
            set { SetProperty(() => OrderPromoCode, value); }
        }

        public int Available
        {
            get { return GetProperty(() => Available); }
            set { SetProperty(() => Available, value); }
        }

        public int CreateQuantity
        {
            get { return GetProperty(() => CreateQuantity); }
            set { SetProperty(() => CreateQuantity, value); }
        }

        public List<OrderProductSnDto> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value); }
        }
    }
}
