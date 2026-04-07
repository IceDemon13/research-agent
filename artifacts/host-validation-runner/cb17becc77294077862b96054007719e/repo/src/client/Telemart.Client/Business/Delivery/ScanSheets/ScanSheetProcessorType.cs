using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.Business.Delivery.ScanSheets
{
    public enum ScanSheetProcessorType
    {
        [Display(Name = "Новая почта")]
        Novaposhta,

        [Display(Name = "Meest Express")]
        MeestExpress,

        [Display(Name = "Телемарт")]
        Telemart,

        [Display(Name = "УкрПочта")]
        Ukrposhta,
    }
}