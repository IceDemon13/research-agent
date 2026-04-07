using System.ComponentModel.DataAnnotations;
using DevExpress.Mvvm.DataAnnotations;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    public enum PurchaseSourceSucceedInTimeState
    {
        [Image("pack://application:,,,/Telemart.Client;component/Images/Common/bullet_green_16x16.png")]
        [Display(Name = "Успевает")]
        Ok,

        [Image("pack://application:,,,/Telemart.Client;component/Images/Common/bullet_yellow_16x16.png")]
        [Display(Name = "Не успевает в старую дату")]
        Warning,

        [Image("pack://application:,,,/Telemart.Client;component/Images/Common/bullet_red_16x16.png")]
        [Display(Name = "Не успевает в срок")]
        Error
    }
}
