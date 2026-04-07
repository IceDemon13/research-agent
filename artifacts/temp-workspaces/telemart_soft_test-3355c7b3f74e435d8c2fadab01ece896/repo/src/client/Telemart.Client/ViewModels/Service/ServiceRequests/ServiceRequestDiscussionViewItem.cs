using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public class ServiceRequestDiscussionViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Message
        {
            get { return GetProperty(() => Message); }
            set { SetProperty(() => Message, value); }
        }

        public string Author
        {
            get { return GetProperty(() => Author); }
            set { SetProperty(() => Author, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public bool ByServiceManager
        {
            get { return GetProperty(() => ByServiceManager); }
            set { SetProperty(() => ByServiceManager, value); }
        }
    }
}