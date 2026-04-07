using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public sealed class AssemblyTestsViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string Suffix
        {
            get { return GetProperty(() => Suffix); }
            set { SetProperty(() => Suffix, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value); }
        }

        public bool AvailOnWeb
        {
            get { return GetProperty(() => AvailOnWeb); }
            set { SetProperty(() => AvailOnWeb, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public int GroupId
        {
            get { return GetProperty(() => GroupId); }
            set { SetProperty(() => GroupId, value); }
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value, () => RaisePropertiesChanged(nameof(IsValueChanged), nameof(IsValid))); }
        }

        public string Regex
        {
            get { return GetProperty(() => Regex); }
            set { SetProperty(() => Regex, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public bool IsValid => Regex is null || Value is null || System.Text.RegularExpressions.Regex.IsMatch(Value, Regex);

        public string OriginalValue { get; set; }

        public bool IsValueChanged => Value != OriginalValue;

        public static void BuildMetadata(MetadataBuilder<AssemblyTestsViewItem> builder)
        {
            builder.Property(x => x.Value)
                .MatchesInstanceRule((x, y) => y.IsValid, () => "Результат теста не соответствует валидатору");
        }

        public void SetValue(string value, bool required)
        {
            OriginalValue = value;
            Value = value;
            Required = required;
        }
    }
}
