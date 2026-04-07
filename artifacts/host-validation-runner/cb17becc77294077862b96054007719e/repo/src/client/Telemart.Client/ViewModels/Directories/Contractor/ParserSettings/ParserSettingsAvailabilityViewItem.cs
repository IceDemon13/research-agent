using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public sealed class ParserSettingsAvailabilityViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ParserId
        {
            get { return GetProperty(() => ParserId); }
            set { SetProperty(() => ParserId, value); }
        }

        public int? AvailabilityTypeId
        {
            get { return GetProperty(() => AvailabilityTypeId); }
            set { SetProperty(() => AvailabilityTypeId, value); }
        }

        public int? Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public string Avail
        {
            get { return GetProperty(() => Avail); }
            set { SetProperty(() => Avail, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ParserSettingsAvailabilityViewItem> builder)
        {
            builder.Property(x => x.AvailabilityTypeId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Quantity).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Avail).Required(() => Resources.RequiredErrorMessage);
        }
    }
}
