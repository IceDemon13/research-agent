using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.ViewModels.Store
{
    public enum ScanSerialMode
    {
        [Display(Name = "Один SN")]
        Single = 0,

        [Display(Name = "Несколько SN")]
        Several = 1,

        [Display(Name = "Диапазон SN")]
        Range = 2
    }
}