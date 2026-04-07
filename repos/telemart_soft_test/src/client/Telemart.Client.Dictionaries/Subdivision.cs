using System.Collections.Generic;

namespace Telemart.Client.Dictionaries
{
    public sealed class Subdivision : DictionaryItem
    {
        private Subdivision(int id, string name, bool isRetail, bool bufferWarehouseRequired)
            : base(id, name, true)
        {
            IsRetail = isRetail;
            BufferWarehouseRequired = bufferWarehouseRequired;
        }

        public static Subdivision Telemart { get; } = new Subdivision(1, "Телемарт", true, true);

        public static Subdivision Nofelet { get; } = new Subdivision(2, "Нофелет", true, true);

        public static Subdivision Retail { get; } = new Subdivision(3, "Розница", true, true);

        public static Subdivision Wholesale { get; } = new Subdivision(4, "Опт", false, false);

        public bool IsRetail { get; }

        public bool BufferWarehouseRequired { get; }

        public IEnumerable<ServiceRequestRequirement> GetServiceRequestRequirements()
        {
            yield return ServiceRequestRequirement.Repair;

            if (IsRetail)
            {
                yield return ServiceRequestRequirement.Change;
            }

            yield return ServiceRequestRequirement.ReturnMoney;

            yield return ServiceRequestRequirement.TradeIn;
        }
    }
}