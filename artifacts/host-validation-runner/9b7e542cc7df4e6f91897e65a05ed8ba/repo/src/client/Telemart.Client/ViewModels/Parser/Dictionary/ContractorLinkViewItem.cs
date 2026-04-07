using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ContractorLinkViewItem : BindableBase
    {
        public ContractorLinkViewItem(int contractorId, string contractor, string link)
        {
            ContractorId = contractorId;
            Contractor = contractor;
            Link = link;

            Uri uri;
            Valid = !string.IsNullOrWhiteSpace(link) && Uri.TryCreate(link, UriKind.Absolute, out uri);
        }

        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            private set { SetProperty(() => ContractorId, value); }
        }

        public string Contractor
        {
            get { return GetProperty(() => Contractor); }
            private set { SetProperty(() => Contractor, value); }
        }

        public string Link
        {
            get { return GetProperty(() => Link); }
            private set { SetProperty(() => Link, value); }
        }

        public bool Valid
        {
            get { return GetProperty(() => Valid); }
            private set { SetProperty(() => Valid, value); }
        }
    }
}