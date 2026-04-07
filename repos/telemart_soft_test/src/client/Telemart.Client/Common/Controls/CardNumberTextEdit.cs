using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public partial class CardNumberTextEdit : TextEdit
    {
        [GeneratedRegex("^\\d{16}")]
        private static partial Regex CardNumberRegex();

        public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(nameof(Required), typeof(bool), typeof(CardNumberTextEdit), new PropertyMetadata(default(bool)));

        public bool Required
        {
            get { return (bool)GetValue(RequiredProperty); }
            set { SetValue(RequiredProperty, value); }
        }

        public CardNumberTextEdit()
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
            string number = e.Value?.ToString();

            if (string.IsNullOrWhiteSpace(number))
            {
                return;
            }

            if (number.Length != 16)
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Default;
                e.ErrorContent = "Длина номера карты должна быть 16 символов";
                e.Handled = true;
                return;
            }

            if (!CardNumberRegex().IsMatch(number))
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Default;
                e.ErrorContent = "Номер карты должен состоять из цифр";
                e.Handled = true;
                return;
            }

            IWebClient webClient = DiContainer.ServiceProvider?.GetService<IWebClient>();

            if (webClient?.IsOperationAllowed(BusinessOperation.DisableRequisitesValidation) != true)
            {
                int sum = 0;
                int mult = 2;

                for (int i = number.Length - 2; i >= 0; i--)
                {
                    int digit = number[i] - '0';

                    int digitMult = digit * mult;

                    sum += digitMult > 9
                        ? digitMult - 9
                        : digitMult;

                    mult = mult == 2 ? 1 : 2;
                }

                if (((10 - (sum % 10)) % 10) != (number[15] - '0'))
                {
                    e.IsValid = false;
                    e.ErrorType = ErrorType.Default;
                    e.ErrorContent = "Номер карты не валидный по контрольной сумме";
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}