using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Common
{
    public class ReorderItemsParameter
    {
        public ReorderItemsParameter(string title, IEnumerable<ComboBoxItem> items, bool allowSorting = true, Func<IReadOnlyCollection<ComboBoxItem>, Task<bool>> okCommand = null)
        {
            Items = items;
            Title = title;
            AllowSorting = allowSorting;
            OkCommand = okCommand;
        }

        public IEnumerable<ComboBoxItem> Items { get; set; }

        public string Title { get; set; }

        public bool AllowSorting { get; set; }

        public Func<IReadOnlyCollection<ComboBoxItem>, Task<bool>> OkCommand { get; set; }
    }
}
