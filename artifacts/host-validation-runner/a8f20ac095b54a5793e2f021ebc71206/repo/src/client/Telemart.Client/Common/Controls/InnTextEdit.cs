using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Validation;
using DevExpress.XtraEditors.DXErrorProvider;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Business;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;

namespace Telemart.Client.Common.Controls
{
    public partial class InnTextEdit : TextEdit
    {
        [GeneratedRegex("^\\d{10}$")]
        private static partial Regex InnRegex();

        private static DateTime StartDate = new DateTime(1899, 12, 31);

        public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(nameof(Required), typeof(bool), typeof(InnTextEdit), new PropertyMetadata(default(bool)));

        public bool Required
        {
            get { return (bool)GetValue(RequiredProperty); }
            set { SetValue(RequiredProperty, value); }
        }

        public InnTextEdit()
        {
            InvalidValueBehavior = InvalidValueBehavior.AllowLeaveEditor;
            Validate += OnValidate;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Validate -= OnValidate;
            Unloaded -= OnUnloaded;
        }

        private void OnValidate(object sender, ValidationEventArgs e)
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

            IWebClient webClient = DiContainer.ServiceProvider?.GetService<IWebClient>();

            if (webClient?.IsOperationAllowed(BusinessOperation.DisableRequisitesValidation) != true)
            {
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
            }

            int birthdayDays = int.Parse(inn[..5]);

            ToolTip = $"Дата рождения: {StartDate.AddDays(birthdayDays).ToString(DateFormattingRules.DateFormat)}, " +
                             $"Пол: {((inn[8] - '0') % 2 == 0 ? "женский" : "мужской")}. Информация может быть не точной";
        }
    }
}