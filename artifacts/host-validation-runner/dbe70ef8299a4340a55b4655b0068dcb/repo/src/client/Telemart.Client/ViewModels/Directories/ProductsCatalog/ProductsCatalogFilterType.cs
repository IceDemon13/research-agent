using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public enum ProductsCatalogFilterType
    {
        [Display(Name = "Все записи (Alt+F)")]
        All = 0,

        [Display(Name = "Только измененные записи (Alt+F)")]
        Changed = 1,

        [Display(Name = "Только записи с ошибками (Alt+F)")]
        WithErrors = 2
    }
}
