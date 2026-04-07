using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class ContractorViewMessage : EditorParameter
    {
        public ContractorViewMessage(int id)
            : base(id)
        {
        }

        public bool IsFolder { get; set; }

        public int? ParentId { get; set; }

        public int? CityId { get; set; }

        public Subdivision Subdivision { get; set; }

        public int? EmployeeId { get; set; }

        public bool IsCompetitor { get; set; }

        public bool OpenOnLogisticsTab { get; init; }
    }
}