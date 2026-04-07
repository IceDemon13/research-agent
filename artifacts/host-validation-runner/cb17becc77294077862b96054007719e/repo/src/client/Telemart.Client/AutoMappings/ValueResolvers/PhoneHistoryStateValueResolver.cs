using System;
using AutoMapper;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.History.Phone;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public class PhoneHistoryStateValueResolver : IMemberValueResolver<PhoneHistoryDto, PhoneHistoryViewItem, int, DictionaryItem>
    {
        public PhoneHistoryStateValueResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public DictionaryItem Resolve(PhoneHistoryDto source, PhoneHistoryViewItem destination, int sourceMember, DictionaryItem destMember, ResolutionContext context)
        {
            DictionaryItem state;

            switch (source.TypeId)
            {
                case PhoneHistoryType.ComplaintId:
                    state = Dictionaries.GetItemById<ComplaintState>(source.StateId);
                    break;
                case PhoneHistoryType.CallId:
                    state = Dictionaries.GetItemById<CallState>(source.StateId);
                    break;
                case PhoneHistoryType.OrderId:
                    state = Dictionaries.GetItemById<OrderStatus>(source.StateId);
                    break;
                case PhoneHistoryType.ServiceRequestId:
                    state = Dictionaries.GetItemById<ServiceRequestState>(source.StateId);
                    break;
                case PhoneHistoryType.TradeInId:
                    state = Dictionaries.GetItemById<TradeInState>(source.StateId);
                    break;
                default:
                    throw new NotSupportedException("Phone history type not supported");
            }

            return state;
        }
    }
}
