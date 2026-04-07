using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public class ServiceRequestSerialNumberViewItem : TelemartViewItemBase
    {
        public ServiceRequestSerialNumberViewItem(string serialNumber, bool warrantyRemoved)
        {
            SerialNumber = serialNumber;
            WarrantyRemoved = warrantyRemoved;
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public bool WarrantyRemoved
        {
            get { return GetProperty(() => WarrantyRemoved); }
            set { SetProperty(() => WarrantyRemoved, value); }
        }
    }
}