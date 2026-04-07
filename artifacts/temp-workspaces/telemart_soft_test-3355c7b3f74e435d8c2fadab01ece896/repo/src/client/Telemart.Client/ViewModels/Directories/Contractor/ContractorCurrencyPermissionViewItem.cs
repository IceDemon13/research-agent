using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public class ContractorCurrencyPermissionViewItem : TelemartCloneableViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public bool Sale
        {
            get { return GetProperty(() => Sale); }
            set { SetProperty(() => Sale, value); }
        }

        public bool Purchase
        {
            get { return GetProperty(() => Purchase); }
            set { SetProperty(() => Purchase, value); }
        }

        public bool CurrencyControl
        {
            get { return GetProperty(() => CurrencyControl); }
            set { SetProperty(() => CurrencyControl, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ContractorCurrencyPermissionViewItem> builder)
        {
            builder.Property(x => x.CurrencyId)
                .MatchesInstanceRule((x, y) => y.Sale || y.Purchase, () => "У валюты должно быть задано хотя бы одно разрешение");
        }
    }
}