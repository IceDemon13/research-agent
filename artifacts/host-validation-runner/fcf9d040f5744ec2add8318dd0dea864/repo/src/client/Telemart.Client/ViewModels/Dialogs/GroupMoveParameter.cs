using System.Collections.ObjectModel;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Dialogs
{
    public class GroupMoveParameter
    {
        public GroupMoveParameter(ReadOnlyObservableCollection<HierarchicalItem> groups, string title)
        {
            Groups = groups;
            Title = title;
        }

        public ReadOnlyObservableCollection<HierarchicalItem> Groups { get; private set; }

        public string Title { get; private set; }
    }
}