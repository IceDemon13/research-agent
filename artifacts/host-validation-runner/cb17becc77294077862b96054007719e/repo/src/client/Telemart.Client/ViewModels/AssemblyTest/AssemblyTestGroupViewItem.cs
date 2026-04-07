using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Common.TreeStructure;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public class AssemblyTestGroupViewItem : AssemblyTestGroupSimpleViewItem, ITreeObject<AssemblyTestGroupViewItem>, ILineObject
    {
        public AssemblyTestGroupViewItem()
        {
            Tests = new ObservableCollection<AssemblyTestsViewItem>();
            Groups = new ObservableCollection<AssemblyTestGroupViewItem>();
        }

        public ObservableCollection<AssemblyTestsViewItem> Tests
        {
            get
            {
                return GetProperty(() => Tests);
            }

            set
            {
                value.CollectionChanged += RaiseChildren;
                SetProperty(() => Tests, value);
            }
        }

        public ObservableCollection<AssemblyTestGroupViewItem> Groups
        {
            get
            {
                return GetProperty(() => Groups);
            }

            set
            {
                value.CollectionChanged += RaiseChildren;
                SetProperty(() => Groups, value, () => RaisePropertyChanged(nameof(Children)));
            }
        }

        public IEnumerable Children => GetChildren().SelectMany(x => x);

        public ICollection<AssemblyTestGroupViewItem> GetChildObjects()
        {
            return Groups;
        }

        public int GetId()
        {
            return Id;
        }

        public int? GetParentId()
        {
            return ParentId;
        }

        public void SetChildObjects(IEnumerable<AssemblyTestGroupViewItem> objects)
        {
            Groups = objects.ToObservableCollection();
        }

        private IEnumerable<IEnumerable<object>> GetChildren()
        {
            if (Tests?.Count > 0)
            {
                yield return Tests;
            }

            if (Groups?.Count > 0)
            {
                yield return Groups;
            }
        }

        private void RaiseChildren(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(Children));
        }
    }
}
