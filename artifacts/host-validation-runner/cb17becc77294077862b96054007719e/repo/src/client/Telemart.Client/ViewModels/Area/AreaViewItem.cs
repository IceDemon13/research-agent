using System.Collections.Generic;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Area
{
    public class AreaViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public string NpAreaRef
        {
            get { return GetProperty(() => NpAreaRef); }
            set { SetProperty(() => NpAreaRef, value); }
        }

        public string MeAreaRef
        {
            get { return GetProperty(() => MeAreaRef); }
            set { SetProperty(() => MeAreaRef, value); }
        }

        public List<object> UpAreaIds
        {
            get { return GetProperty(() => UpAreaIds); }
            set { SetProperty(() => UpAreaIds, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AreaViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUkr)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}