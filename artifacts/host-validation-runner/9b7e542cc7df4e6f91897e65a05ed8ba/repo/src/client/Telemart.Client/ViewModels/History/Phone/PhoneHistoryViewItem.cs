using System;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.History.Phone
{
    public class PhoneHistoryViewItem : BindableBase
    {
        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public DictionaryItem State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int DocumentNumber
        {
            get { return GetProperty(() => DocumentNumber); }
            set { SetProperty(() => DocumentNumber, value); }
        }

        public string Phone1
        {
            get { return GetProperty(() => Phone1); }
            set { SetProperty(() => Phone1, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool IsLastPhone
        {
            get { return GetProperty(() => IsLastPhone); }
            set { SetProperty(() => IsLastPhone, value); }
        }

        public int? CustomerId
        {
            get { return GetProperty(() => CustomerId); }
            set { SetProperty(() => CustomerId, value); }
        }

        public bool IsNotCompletedState
        {
            get
            {
                switch (TypeId)
                {
                    case PhoneHistoryType.OrderId:
                        return !(State.Id == OrderStatus.Done.Id || State.Id == OrderStatus.DidNotTake.Id || State.Id == OrderStatus.DidNotOrder.Id || State.Id == OrderStatus.Canceled.Id || State.Id == OrderStatus.Returned.Id);
                    case PhoneHistoryType.ServiceRequestId:
                        return !(State.Id == ServiceRequestState.Completed.Id || State.Id == ServiceRequestState.Cancelled.Id);
                    case PhoneHistoryType.CallId:
                        return !(State.Id == CallState.SolvedId || State.Id == CallState.NotSolvedId || State.Id == CallState.CanceledId || State.Id == CallState.NotReachedId);
                    case PhoneHistoryType.ComplaintId:
                        return State.Id == ComplaintState.New.Id;
                    case PhoneHistoryType.TradeInId:
                        return !(State.Id == TradeInState.Completed.Id || State.Id == TradeInState.Canceled.Id);
                    default:
                        return true;
                }
            }
        }
    }
}
