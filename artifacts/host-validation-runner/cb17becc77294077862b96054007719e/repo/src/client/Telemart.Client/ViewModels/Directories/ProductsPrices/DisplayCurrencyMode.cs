using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public enum DisplayCurrencyMode
    {
        [Display(Name = "В валюте цены")]
        Default = 0,

        [Display(Name = "В валюте товара")]
        Product = 1,

        [Display(Name = "В гривне")]
        Uah = 2,

        [Display(Name = "В долларах")]
        Usd = 3,

        [Display(Name = "В евро")]
        Eur = 4
    }
}