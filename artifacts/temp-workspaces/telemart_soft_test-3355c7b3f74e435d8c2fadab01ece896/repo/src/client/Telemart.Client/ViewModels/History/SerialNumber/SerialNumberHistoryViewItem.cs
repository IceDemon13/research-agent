using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.History.SerialNumber
{
    public class SerialNumberHistoryViewItem : BindableBase
    {
        public int DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public string DocumentType
        {
            get { return GetProperty(() => DocumentType); }
            set { SetProperty(() => DocumentType, value); }
        }

        public string DocumentTypeRu
        {
            get { return GetProperty(() => DocumentTypeRu); }
            set { SetProperty(() => DocumentTypeRu, value); }
        }

        public DateTime? DateTime
        {
            get { return GetProperty(() => DateTime); }
            set { SetProperty(() => DateTime, value); }
        }

        public string Info
        {
            get { return GetProperty(() => Info); }
            set { SetProperty(() => Info, value); }
        }
    }
}