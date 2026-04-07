using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Locations
{
    public sealed class ClusterViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int[] LocationIds
        {
            get { return GetProperty(() => LocationIds); }
            set { SetProperty(() => LocationIds, value); }
        }

        public ObservableCollection<LocationViewItem> Locations
        {
            get { return GetProperty(() => Locations); }
            set { SetProperty(() => Locations, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ClusterViewItem> b)
        {
            b.Property(x => x.Name).MatchesRule(x => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Длина поля должна быть не больше чем 100  символов");
        }

        public int[] GetLocationIds()
        {
            return Locations.Select(x => x.Id).ToArray();
        }
    }
}