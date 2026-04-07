using System.Collections.ObjectModel;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public sealed class GridBandExItem : GridBandItem
    {
        public GridBandExItem(string name, bool isFixed = false)
            : base(name, isFixed)
        {
            Bands = new ObservableCollection<GridBandItem>();
        }

        public ObservableCollection<GridBandItem> Bands { get; }
    }
}