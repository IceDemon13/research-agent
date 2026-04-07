using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.Common.Settings.Equipment
{
    public enum PosType
    {
        [Display(Name = "ПриватБанк")]
        PrivatBank = 1,

        [Display(Name = "Ощадбанк (Ingenico)")]
        Ingenico = 2,

        [Display(Name = "Укрсиббанк (Ingenico)")]
        Ukrsibbank = 3
    }
}