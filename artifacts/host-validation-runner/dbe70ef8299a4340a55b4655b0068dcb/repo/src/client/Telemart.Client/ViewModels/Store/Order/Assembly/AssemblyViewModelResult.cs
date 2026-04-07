using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Store.Order.Assembly
{
    public class AssemblyViewModelResult
    {
        public AssemblyViewModelResult(NomenclatureViewItem nomenclatureItem, bool assemblyIncluded, bool isGift)
        {
            NomenclatureItem = nomenclatureItem;
            AssemblyIncluded = assemblyIncluded;
            IsGift = isGift;
        }

        public NomenclatureViewItem NomenclatureItem { get; set; }

        public bool IsGift { get; }

        public bool AssemblyIncluded { get; set; }
    }
}