using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public sealed class ParserSettingsParameter : EditorParameter
    {
        public ParserSettingsParameter(int id, int? contractorId = null)
            : base(id)
        {
            ContractorId = contractorId;
        }

        public int? ContractorId { get; }
    }
}