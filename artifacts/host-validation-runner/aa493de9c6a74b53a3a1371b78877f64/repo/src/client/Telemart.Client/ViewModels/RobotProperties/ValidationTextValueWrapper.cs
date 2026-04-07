using System.Text.RegularExpressions;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Style.Editors;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class ValidationTextValueWrapper : ValueWrapper<string>
    {
        private readonly string _propertyRegex;

        public ValidationTextValueWrapper(string value, string propertyRegex, bool active, bool isReadOnly = false)
            : base(value, isReadOnly)
        {
            Active = active;
            _propertyRegex = propertyRegex;
        }

        public ValidationTextValueWrapper(string value, bool isReadOnly = false)
            : this(value, null, false, isReadOnly)
        {
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertyChanged(nameof(Value))); }
        }

        public string PropertyRegex => _propertyRegex;

        public static void BuildMetadata(MetadataBuilder<ValidationTextValueWrapper> builder)
        {
            builder.Property(x => x.Value)
                .MatchesInstanceRule((x, y) => y.IsReadOnly || !y.Active || !string.IsNullOrEmpty(x), () => "Значение обязательно к заполнению")
                .MatchesInstanceRule(
                    (x, y) => y.IsReadOnly || string.IsNullOrEmpty(y.PropertyRegex) || string.IsNullOrEmpty(x) || Regex.IsMatch(x, y.PropertyRegex),
                    () => "Переменная имеет шаблон. Значение не соответствует шаблону");
        }
    }
}