using System;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using DevExpress.XtraEditors.DXErrorProvider;
using Telemart.Client.Business;

namespace Telemart.Client.Helpers
{
    public static partial class RequisitesValidationHelper
    {
        [GeneratedRegex("^\\d+$")]
        private static partial Regex InnRegex();

        private static DateTime StartDate = new DateTime(1899, 12, 31);

        public static IDelegateCommand InnValidateCommand = new DelegateCommand<ValidationEventArgs>(x => InnValidate(x));

        public static void InnValidate(ValidationEventArgs e)
        {
            string inn = e.Value?.ToString();

            if (string.IsNullOrWhiteSpace(inn))
            {
                return;
            }

            if (inn.Length != 10)
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Default;
                e.ErrorContent = "Длина ИНН должна быть 10 цифр";
                e.Handled = true;
                return;
            }

            if (!InnRegex().IsMatch(inn))
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Default;
                e.ErrorContent = "ИНН должен состоять из цифр";
                e.Handled = true;
                return;
            }

            int[] d = inn.Select(x => int.Parse(x.ToString())).ToArray();

            int sum = (d[0] * -1) +
                      (d[1] * 5) +
                      (d[2] * 7) +
                      (d[3] * 9) +
                      (d[4] * 4) +
                      (d[5] * 6) +
                      (d[6] * 10) +
                      (d[7] * 5) +
                      (d[8] * 7);

            if ((sum % 11) % 10 != d[9])
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Default;
                e.ErrorContent = "ИНН не валидный по контрольной сумме";
                e.Handled = true;
                return;
            }

            int birthdayDays = int.Parse(inn[..5]);

            e.IsValid = false;
            e.ErrorType = ErrorType.Information;
            e.ErrorContent = $"Дата рождения: {StartDate.AddDays(birthdayDays).ToString(DateFormattingRules.DateFormat)}, " +
                             $"Пол: {(d[8] % 2 == 0 ? "женский" : "мужской")}. Информация может быть не точной";
            e.Handled = true;
        }
    }
}