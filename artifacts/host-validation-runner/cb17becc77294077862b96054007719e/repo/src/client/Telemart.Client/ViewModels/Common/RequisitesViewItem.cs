using System.Text.RegularExpressions;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Validation;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public class RequisitesViewItem : TelemartViewItemBase
    {
        public bool Visible
        {
            get { return GetProperty(() => Visible); }
            set { SetProperty(() => Visible, value); }
        }

        public bool Enabled
        {
            get { return GetProperty(() => Enabled); }
            set { SetProperty(() => Enabled, value, EnabledChanged); }
        }

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value, RequiredChanged); }
        }

        public string FirstName
        {
            get { return GetProperty(() => FirstName); }
            set { SetProperty(() => FirstName, value); }
        }

        public string LastName
        {
            get { return GetProperty(() => LastName); }
            set { SetProperty(() => LastName, value); }
        }

        public string MiddleName
        {
            get { return GetProperty(() => MiddleName); }
            set { SetProperty(() => MiddleName, value); }
        }

        public string Iban
        {
            get { return GetProperty(() => Iban); }
            set { SetProperty(() => Iban, value); }
        }

        public string CardNumber
        {
            get { return GetProperty(() => CardNumber); }
            set { SetProperty(() => CardNumber, value); }
        }

        public string Inn
        {
            get { return GetProperty(() => Inn); }
            set { SetProperty(() => Inn, value); }
        }

        public static void BuildMetadata(MetadataBuilder<RequisitesViewItem> builder)
        {
            builder.Property(x => x.FirstName)
                .MatchesInstanceRule(
                    (x, y) => (!y.Required && string.IsNullOrEmpty(x)) || (!string.IsNullOrEmpty(x) && x.Length < 20),
                    () => "Имя не может быть длиннее 20 символов")
                .ApplyCyrillicValidationRules();
            builder.Property(x => x.LastName)
                .MatchesInstanceRule(
                    (x, y) => (!y.Required && string.IsNullOrEmpty(x)) || (!string.IsNullOrEmpty(x) && x.Length < 20),
                    () => "Фамилия не может быть длиннее 25 символов")
                .ApplyCyrillicValidationRules();
            builder.Property(x => x.MiddleName)
                .MatchesInstanceRule(
                    (x, y) => (!y.Required && string.IsNullOrEmpty(x)) || (!string.IsNullOrEmpty(x) && x.Length < 25),
                    () => "Отчество не может быть длиннее 25 символов")
                .ApplyCyrillicValidationRules();
            builder.Property(x => x.Inn)
                .MatchesInstanceRule(
                    (x, y) => !y.Required || !string.IsNullOrEmpty(x),
                    () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CardNumber)
                .MatchesInstanceRule(
                    (x, y) => (!y.Required && string.IsNullOrEmpty(x)) ||
                              (!string.IsNullOrEmpty(x) && x.Length == 16 && Regex.IsMatch(x, @"^\d+$")),
                    () => "Длина номера карты должна быть 16 цифр");
            builder.Property(x => x.Iban)
                .MatchesInstanceRule(
                    (x, y) => (!y.Required && string.IsNullOrEmpty(x)) || (!string.IsNullOrEmpty(x) && x.Length == 29),
                    () => "Длина IBAN должна составлять 29 символов");
        }

        private void RequiredChanged()
        {
            if (Required)
            {
                Visible = true;
            }

            RaiseProperties();
        }

        private void EnabledChanged()
        {
            if (Enabled)
            {
                Visible = true;
            }

            RaiseProperties();
        }

        private void RaiseProperties()
        {
            RaisePropertiesChanged(
                nameof(FirstName),
                nameof(LastName),
                nameof(MiddleName),
                nameof(Inn),
                nameof(Iban),
                nameof(CardNumber));
        }
    }
}