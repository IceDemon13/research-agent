using DevExpress.Mvvm;

namespace Telemart.Client.Common.Behaviors
{
    internal abstract class LayoutControlItem : BindableBase
    {
        public string Label { get; set; }
    }
}