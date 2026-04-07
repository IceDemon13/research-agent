using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInEDocumentViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int TradeInId
        {
            get { return GetProperty(() => TradeInId); }
            set { SetProperty(() => TradeInId, value); }
        }

        public int DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public string KeyDocument
        {
            get { return GetProperty(() => KeyDocument); }
            set { SetProperty(() => KeyDocument, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Url
        {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value); }
        }

        public DateTime? SendOn
        {
            get { return GetProperty(() => SendOn); }
            set { SetProperty(() => SendOn, value, () => RaisePropertyChanged(nameof(Sent))); }
        }

        public int? SendBy
        {
            get { return GetProperty(() => SendBy); }
            set { SetProperty(() => SendBy, value); }
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

        public bool AcceptedClient
        {
            get { return GetProperty(() => AcceptedClient); }
            set { SetProperty(() => AcceptedClient, value); }
        }

        public bool AcceptedTelemart
        {
            get { return GetProperty(() => AcceptedTelemart); }
            set { SetProperty(() => AcceptedTelemart, value); }
        }

        public string Status
        {
            get { return GetProperty(() => Status); }
            set { SetProperty(() => Status, value); }
        }

        public DateTime? AcceptedTelemartOn
        {
            get { return GetProperty(() => AcceptedTelemartOn); }
            set { SetProperty(() => AcceptedTelemartOn, value); }
        }

        public bool Sent => SendOn.HasValue;
    }
}