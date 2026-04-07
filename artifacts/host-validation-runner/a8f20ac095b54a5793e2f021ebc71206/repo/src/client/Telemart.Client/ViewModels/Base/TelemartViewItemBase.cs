using System.ComponentModel;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Base
{
    public abstract class TelemartViewItemBase : BindableBase, IDataErrorInfo
    {
        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);
    }
}
