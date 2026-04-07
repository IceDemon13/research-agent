using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Validation;
using DevExpress.XtraEditors.DXErrorProvider;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;

namespace Telemart.Client.Common.Controls
{
    public partial class IbanTextEdit : TextEdit
    {
        [GeneratedRegex("^\\D{2}\\d{27}")]
        private static partial Regex IbanRegex();

        private static IReadOnlyDictionary<char, string> charsToNumber = new Dictionary<char, string>()
        {
            ['A'] = "10",
            ['B'] = "11",
            ['C'] = "12",
            ['D'] = "13",
            ['E'] = "14",
            ['F'] = "15",
            ['G'] = "16",
            ['H'] = "17",
            ['I'] = "18",
            ['J'] = "19",
            ['K'] = "20",
            ['L'] = "21",
            ['M'] = "22",
            ['N'] = "23",
            ['O'] = "24",
            ['P'] = "25",
            ['Q'] = "26",
            ['R'] = "27",
            ['S'] = "28",
            ['T'] = "29",
            ['U'] = "30",
            ['V'] = "31",
            ['W'] = "32",
            ['X'] = "33",
            ['Y'] = "34",
            ['Z'] = "35"
        };

        public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(nameof(Required), typeof(bool), typeof(IbanTextEdit), new PropertyMetadata(default(bool)));

        public bool Required
        {
            get { return (bool)GetValue(RequiredProperty); }
            set { SetValue(RequiredProperty, value); }
        }

        public IbanTextEdit()
        {
            CharacterCasing = CharacterCasing.Upper;
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
            string iban = e.Value?.ToString();

            if (string.IsNullOrWhiteSpace(iban))
            {
                return;
            }

            if (iban.Length != 29)
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Default;
                e.ErrorContent = "Длина IBAN должна быть 29 символов";
                e.Handled = true;
                return;
            }

            if (!IbanRegex().IsMatch(iban))
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Default;
                e.ErrorContent = "IBAN должен начинаться с кода страны";
                e.Handled = true;
                return;
            }

            StringBuilder sb = new StringBuilder(29);

            sb.Append(iban[4..]);

            foreach (char c in iban[..4])
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(charsToNumber[Char.ToUpper(c)]);
                }
            }

            IWebClient webClient = DiContainer.ServiceProvider?.GetService<IWebClient>();

            if (webClient?.IsOperationAllowed(BusinessOperation.DisableRequisitesValidation) != true)
            {
                BigInteger ibanInt = BigInteger.Parse(sb.ToString());

                BigInteger.DivRem(ibanInt, 97, out var rem);

                if (rem != 1)
                {
                    e.IsValid = false;
                    e.ErrorType = ErrorType.Default;
                    e.ErrorContent = "IBAN не валидный по контрольной сумме";
                    e.Handled = true;
                    return;
                }
            }

            ToolTip = $"МФО: {iban[4..10]}";
        }
    }
}