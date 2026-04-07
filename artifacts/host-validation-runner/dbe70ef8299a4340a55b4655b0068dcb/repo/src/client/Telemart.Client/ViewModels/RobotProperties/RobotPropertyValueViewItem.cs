using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class RobotPropertyValueViewItem : TelemartViewItemBase
    {
        public RobotPropertyValueViewItem()
        {
        }

        public RobotPropertyValueViewItem(int id, int robotPropertyId, string displayName, string value)
        {
            Id = id;
            RobotPropertyId = robotPropertyId;
            DisplayName = displayName;
            Value = value;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int RobotPropertyId
        {
            get { return GetProperty(() => RobotPropertyId); }
            set { SetProperty(() => RobotPropertyId, value); }
        }

        public string DisplayName
        {
            get { return GetProperty(() => DisplayName); }
            set { SetProperty(() => DisplayName, value); }
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }

        public static void BuildMetadata(MetadataBuilder<RobotPropertyValueViewItem> builder)
        {
            builder.Property(x => x.DisplayName).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Value).Required(() => Resources.RequiredErrorMessage);
        }
    }
}