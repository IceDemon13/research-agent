using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups.FeatureValues
{
    public sealed class FeatureValueViewItem : BindableBase, IDataErrorInfo
    {
        private readonly string oldValue;
        private readonly string oldValueUkr;
        private readonly string oldValueEn;

        private readonly string oldUrl;
        private readonly string oldUrlUkr;
        private readonly string oldUrlEn;

        public FeatureValueViewItem(
            int id,
            int featureId,
            string value,
            string valueUkr,
            string valueEn,
            string url,
            string urlUkr,
            string urlEn,
            string regex,
            bool multiLanguage)
        {
            Value = oldValue = value;
            ValueUkr = oldValueUkr = valueUkr;
            ValueEn = oldValueEn = valueEn;

            Url = oldUrl = url;
            UrlUkr = oldUrlUkr = urlUkr;
            UrlEn = oldUrlEn = urlEn;

            Id = id;

            MultiLanguage = multiLanguage;
            FeatureId = featureId;
            Regex = regex;
        }

        public FeatureValueViewItem(int featureId, string regex, string value, int languageId, bool multiLanguage, string url = null)
        {
            switch (languageId)
            {
                case Language.UkrainianId:
                {
                    ValueUkr = oldValueUkr = value;
                    UrlUkr = oldUrlUkr = url;

                    if (multiLanguage)
                    {
                        Value = oldValue = value;
                        Url = oldUrl = url;
                        ValueEn = oldValueEn = value;
                        UrlEn = oldUrlEn = url;
                    }

                    break;
                }

                case Language.RussianId:
                {
                    Value = oldValue = value;
                    Url = oldUrl = url;

                    if (multiLanguage)
                    {
                        ValueUkr = oldValueUkr = value;
                        UrlUkr = oldUrlUkr = url;
                        ValueEn = oldValueEn = value;
                        UrlEn = oldUrlEn = url;
                    }

                    break;
                }

                case Language.EnglishId:
                {
                    ValueEn = oldValueEn = value;
                    UrlEn = oldUrlEn = url;

                    if (multiLanguage)
                    {
                        ValueUkr = oldValueUkr = value;
                        UrlUkr = oldUrlUkr = url;
                        Value = oldValue = value;
                        Url = oldUrl = url;
                    }

                    break;
                }
            }

            MultiLanguage = multiLanguage;
            FeatureId = featureId;
            Regex = regex;
        }

        public int Id { get; }

        public int FeatureId { get; }

        public string Regex { get; }

        public bool MultiLanguage { get; }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value, () => RaisePropertiesChanged(nameof(IsChanged), nameof(ValueUkr), nameof(ValueEn))); }
        }

        public string ValueUkr
        {
            get { return MultiLanguage ? Value : GetProperty(() => ValueUkr); }
            set { SetProperty(() => ValueUkr, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public string ValueEn
        {
            get { return MultiLanguage ? Value : GetProperty(() => ValueEn); }
            set { SetProperty(() => ValueEn, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public string DisplayValue
        {
            get { return GetProperty(() => DisplayValue); }
            private set { SetProperty(() => DisplayValue, value); }
        }

        public string Url
        {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value, () => RaisePropertiesChanged(nameof(IsChanged), nameof(UrlUkr), nameof(UrlEn))); }
        }

        public string UrlUkr
        {
            get { return MultiLanguage ? Url : GetProperty(() => UrlUkr); }
            set { SetProperty(() => UrlUkr, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public string UrlEn
        {
            get { return MultiLanguage ? Url : GetProperty(() => UrlEn); }
            set { SetProperty(() => UrlEn, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public bool IsChanged => Id == 0
                                 || !string.Equals(Value, oldValue, StringComparison.Ordinal)
                                 || !string.Equals(ValueUkr, oldValueUkr, StringComparison.Ordinal)
                                 || !string.Equals(ValueEn, oldValueEn, StringComparison.Ordinal)
                                 || !string.Equals(Url, oldUrl, StringComparison.Ordinal)
                                 || !string.Equals(UrlUkr, oldUrlUkr, StringComparison.Ordinal)
                                 || !string.Equals(UrlEn, oldUrlEn, StringComparison.Ordinal);

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<FeatureValueViewItem> builder)
        {
            builder.Property(x => x.Value)
                .MatchesInstanceRule(
                    (x, y) =>
                        (string.IsNullOrWhiteSpace(y.Regex) && !string.IsNullOrWhiteSpace(x)) ||
                        (!string.IsNullOrWhiteSpace(x) && System.Text.RegularExpressions.Regex.IsMatch(x, y.Regex)),
                    () => "Невалидное значение");

            builder.Property(x => x.ValueUkr)
                 .MatchesInstanceRule(
                     (x, y) =>
                         (string.IsNullOrWhiteSpace(y.Regex) && !string.IsNullOrWhiteSpace(x)) ||
                         (!string.IsNullOrWhiteSpace(x) && System.Text.RegularExpressions.Regex.IsMatch(x, y.Regex)),
                     () => "Невалидное значение");

            builder.Property(x => x.ValueEn)
                .MatchesInstanceRule(
                    (x, y) =>
                        (string.IsNullOrWhiteSpace(y.Regex) && !string.IsNullOrWhiteSpace(x)) ||
                        (!string.IsNullOrWhiteSpace(x) && System.Text.RegularExpressions.Regex.IsMatch(x, y.Regex)),
                    () => "Невалидное значение");
        }

        public string GetValue(int languageId)
        {
            return languageId switch
            {
                Language.RussianId => Value,
                Language.UkrainianId => ValueUkr,
                Language.EnglishId => ValueEn,
                _ => throw new NotSupportedException("Language not supported"),
            };
        }

        public void SetLanguage(int languageId)
        {
            DisplayValue = GetValue(languageId);
        }
    }
}
