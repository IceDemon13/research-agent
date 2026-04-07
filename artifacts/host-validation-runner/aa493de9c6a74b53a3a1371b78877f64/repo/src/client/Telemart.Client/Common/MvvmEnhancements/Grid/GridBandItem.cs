using System.Collections.ObjectModel;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public class GridBandItem
    {
        public GridBandItem(string name, bool isFixed = false)
        {
            BandName = name;
            IsFixed = isFixed;

            Columns = new ObservableCollection<GridColumnItem>();
        }

        public string BandName { get; }

        public bool IsFixed { get; }

        public ObservableCollection<GridColumnItem> Columns { get; }
    }
}