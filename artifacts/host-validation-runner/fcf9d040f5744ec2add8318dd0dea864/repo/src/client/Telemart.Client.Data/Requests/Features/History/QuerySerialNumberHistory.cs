using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.History
{
    public sealed class QuerySerialNumberHistory : QueryEntitiesRequestBase<SerialNumberHistoryDto>
    {
        public QuerySerialNumberHistory(string serialNumber)
            : base(new SerialNumberHistoryFilteringItem(serialNumber), ApiResources.History, "sn")
        {
        }

        private class SerialNumberHistoryFilteringItem : IFilteringItem
        {
            private readonly string _serialNumber;

            public SerialNumberHistoryFilteringItem(string serialNumber)
            {
                _serialNumber = serialNumber;
            }

            public IEnumerable<(string, object)> BuildParameters()
            {
                if (!string.IsNullOrWhiteSpace(_serialNumber))
                {
                    yield return ("sn", _serialNumber.Trim());
                }
            }
        }
    }
}