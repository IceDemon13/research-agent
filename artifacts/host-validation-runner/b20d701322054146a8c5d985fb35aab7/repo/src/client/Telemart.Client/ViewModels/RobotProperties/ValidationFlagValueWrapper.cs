using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Style.Editors;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public sealed class ValidationFlagValueWrapper : ValueWrapper<bool?>
    {
        public ValidationFlagValueWrapper(bool? value, bool active, bool isReadOnly = false)
            : base(value, isReadOnly)
        {
            Active = active;
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertyChanged(nameof(Value))); }
        }

        public static void BuildMetadata(MetadataBuilder<ValidationFlagValueWrapper> builder)
        {
            builder.Property(x => x.Value)
                .MatchesInstanceRule((x, y) => y.IsReadOnly || !y.Active || x.HasValue, () => "Для флага нужно определить значение");
        }
    }
}