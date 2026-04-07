using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public class AssemblyTestGroupSimpleViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUa
        {
            get { return GetProperty(() => NameUa); }
            set { SetProperty(() => NameUa, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public string ParentName
        {
            get { return GetProperty(() => ParentName); }
            set { SetProperty(() => ParentName, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public bool ShowInAssemblyServiceTestGrid
        {
            get { return GetProperty(() => ShowInAssemblyServiceTestGrid); }
            set { SetProperty(() => ShowInAssemblyServiceTestGrid, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AssemblyTestGroupSimpleViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUa)
              .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}
