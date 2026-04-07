using System;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Validation
{
    public static class SharedValidationRules
    {
        private const decimal MaxPackageWeight = 1000m;
        private const decimal TeksMaxPackageWeight = 10000m;

        public static PropertyMetadataBuilder<T, string> ApplyCyrillicValidationRules<T>(this PropertyMetadataBuilder<T, string> builder, Func<string> errorMessageAccessor = null)
        {
            return builder.MatchesRegularExpression(@"[\p{IsCyrillic}\s\.-`']*", errorMessageAccessor ?? (() => "Допускаются только символы русского/украинского алфавита"));
        }

        public static PropertyMetadataBuilder<T, string> ApplyFioValidationRules<T>(this PropertyMetadataBuilder<T, string> builder, Func<string> errorMessageAccessor = null)
        {
            return builder
                .Required(errorMessageAccessor)
                .MaxLength(100, errorMessageAccessor)
                .ApplyCyrillicValidationRules();
        }

        public static PropertyMetadataBuilder<T, string> ApplyFioLatinValidationRules<T>(this PropertyMetadataBuilder<T, string> builder, Func<string> errorMessageAccessor = null)
        {
            return builder
                .MaxLength(100, errorMessageAccessor)
                .Latin();
        }

        public static PropertyMetadataBuilder<T, string> ApplyFioPartValidationRules<T>(this PropertyMetadataBuilder<T, string> builder)
        {
            return builder
                .MaxLength(16, () => "Значение не может быть длинее 16 символов")
                .MatchesRegularExpression(@"[\p{IsCyrillic}`'-]*", () => "Допускаются только символы русского/украинского алфавита");
        }

        public static PropertyMetadataBuilder<T, string> ApplyCategoryNameValidationRules<T>(this PropertyMetadataBuilder<T, string> builder, Func<string> errorMessageAccessor = null)
        {
            return builder
                .Required(() => "Имя категории должно быть заполнено")
                .MatchesRegularExpression(@"^[a-zA-Z0-9А-Яа-я]{1}[a-zA-Z0-9А-Яа-я\s&-./]*$", () => "Вы ввели запрещенные символ(ы)");
        }

        public static PropertyMetadataBuilder<T, string> ApplyCategoryNameUkrValidationRules<T>(this PropertyMetadataBuilder<T, string> builder, Func<string> errorMessageAccessor = null)
        {
            return builder
                .Required(() => "Имя категории должно быть заполнено")
                .MatchesRegularExpression(@"^[ІіЇїЄєҐґa-zA-Z0-9А-Яа-я]{1}[ІіЇїЄєҐґa-zA-Z0-9А-Яа-я\s&-.\/]*$", () => "Вы ввели запрещенные символ(ы)");
        }

        public static PropertyMetadataBuilder<T, string> ApplyCategoryNameEnValidationRules<T>(this PropertyMetadataBuilder<T, string> builder, Func<string> errorMessageAccessor = null)
        {
            return builder
                .Required(() => "Имя категории должно быть заполнено")
                .MatchesRegularExpression(@"^[a-zA-Z0-9]{1}[a-zA-Z0-9\s&-.\/]*$", () => "Вы ввели запрещенные символ(ы)");
        }

        public static PropertyMetadataBuilder<T, string> ApplyClientNameRusUkrValidationRules<T>(this PropertyMetadataBuilder<T, string> builder, Func<string> errorMessageAccessor = null)
        {
            return builder.MatchesRegularExpression(@"^[ІіЇїЄєҐґА-Яа-я]{1}[ІіЇїЄєҐґА-Яа-я\s&-.`'\/]*$", () => "Допускаются только символы русского/украинского алфавита");
        }

        public static PropertyMetadataBuilder<T, string> PasswordLength<T>(this PropertyMetadataBuilder<T, string> builder, int minLength, int maxLength)
        {
            return builder
                .MinLength(minLength, () => $"Пароль должен состоять минимум из {minLength} символов")
                .MaxLength(maxLength, () => $"Пароль не может быть длинее {maxLength} символов");
        }

        public static PropertyMetadataBuilder<T, string> Password<T>(this PropertyMetadataBuilder<T, string> builder)
        {
            return builder
                .MatchesRule(
                    x => string.IsNullOrEmpty(x) || (x.Any(char.IsUpper) && x.Any(char.IsLower) && x.Any(char.IsDigit)),
                    () => "Пароль должен содержать хотя бы одну заглавную букву и цифру")
                .MatchesRule(
                    x => string.IsNullOrEmpty(x) || x.All(z => z.IsBasicLatin() || char.IsDigit(z)),
                    () => "Пароль должен состоять только из символов латиницы и цифр");
        }

        public static PropertyMetadataBuilder<T, string> Cyrilic<T>(this PropertyMetadataBuilder<T, string> builder)
        {
            return builder.MatchesRule(
                x => string.IsNullOrWhiteSpace(x) || x.All(c => c.IsCyrillic()),
                () => "Поле должно содержать только кириллицу");
        }

        public static PropertyMetadataBuilder<T, string> Latin<T>(this PropertyMetadataBuilder<T, string> builder)
        {
            return builder.MatchesRule(
                x => string.IsNullOrWhiteSpace(x) || x.All(c => c.IsBasicLatin()),
                () => "Поле должно содержать только символы латинского алфавита");
        }

        public static PropertyMetadataBuilder<T, string> LatinOrNumber<T>(this PropertyMetadataBuilder<T, string> builder)
        {
            return builder.MatchesRule(
                x => string.IsNullOrWhiteSpace(x) || x.All(c => c.IsBasicLatin() || char.IsNumber(c)),
                () => "Поле должно содержать только символы латинского алфавита и цифры");
        }

        public static PropertyMetadataBuilder<T, string> NumberArray<T>(this PropertyMetadataBuilder<T, string> builder)
        {
            return builder.MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public static PropertyMetadataBuilder<T, decimal> NpPackagePlaces<T>(this PropertyMetadataBuilder<T, decimal> builder)
        {
            const decimal MaxPackagePlases = 99m;
            const decimal MinPackagePlases = 1m;

            return builder.MatchesRule(
                x => x >= MinPackagePlases && x <= MaxPackagePlases,
                () => $"Кол-во мест должно быть в пределах {MinPackagePlases:F0}..{MaxPackagePlases:F0}");
        }

        public static PropertyMetadataBuilder<T, decimal> NpPackageWeight<T>(this PropertyMetadataBuilder<T, decimal> builder)
        {
            const decimal MinPackageWeight = 0.1m;

            return builder.MatchesRule(
                x => x >= MinPackageWeight && x <= MaxPackageWeight,
                () => $"Вес посылки должен быть в пределах {MinPackageWeight:F1}..{MaxPackageWeight:F0}");
        }

        public static PropertyMetadataBuilder<T, decimal> PackageWeight<T>(this PropertyMetadataBuilder<T, decimal> builder)
            where T : ICarrySupport
        {
            const decimal UpMaxPackageWeight = 30m;
            const decimal MinPackageWeight = 0.1m;

            return builder.MatchesInstanceRule(
                (x, y) => x >= MinPackageWeight && ((y.CarryId.IsUpCarryId() && x <= UpMaxPackageWeight)
                                                    || (y.CarryId.IsTeksCarry() && x <= TeksMaxPackageWeight)
                                                    || (!y.CarryId.IsUpCarryId() && !y.CarryId.IsTeksCarry() && x <= MaxPackageWeight)),
                (_, y) =>
                    $"Вес посылки должен быть в пределах {MinPackageWeight:F1}..{(y.CarryId.IsUpCarryId()
                        ? UpMaxPackageWeight.ToString("F0")
                        : y.CarryId.IsTeksCarry()
                            ? TeksMaxPackageWeight.ToString("F0")
                            : MaxPackageWeight.ToString("F0"))}");
        }

        public static PropertyMetadataBuilder<T, int?> NpPostBoxHeight<T>(this PropertyMetadataBuilder<T, int?> builder)
            where T : ICarrySupport
        {
            const int MinHeight = 1;
            const int MaxHeight = 36;

            return builder.MatchesInstanceRule(
                (x, y) => x is null || (x >= MinHeight && (y.CarryId.IsUpCarryId() || x <= MaxHeight)),
                (_, y) => $"Высота посылки должна быть в пределах {MinHeight}..{MaxHeight}");
        }

        public static PropertyMetadataBuilder<T, int?> NpPostBoxWidth<T>(this PropertyMetadataBuilder<T, int?> builder)
            where T : ICarrySupport
        {
            const int MinWidth = 1;
            const int MaxWidth = 40;

            return builder.MatchesInstanceRule(
                (x, y) => x is null || (x >= MinWidth && (y.CarryId.IsUpCarryId() || x <= MaxWidth)),
                (_, y) => $"Ширина посылки должна быть в пределах {MinWidth}..{MaxWidth}");
        }

        public static PropertyMetadataBuilder<T, int?> PlaceLength<T>(this PropertyMetadataBuilder<T, int?> builder)
        where T : ICarrySupport
        {
            const int MinLength = 1;
            const int MaxNpPostBoxLength = 58;
            const int MaxUpLength = 200;

            return builder.MatchesInstanceRule(
                (x, y) => x is null || (x >= MinLength && ((y.CarryId.IsUpCarryId() && x <= MaxUpLength) || (!y.CarryId.IsUpCarryId() && x <= MaxNpPostBoxLength))),
                (_, y) => $"Длина посылки должна быть в пределах {MinLength}..{(y.CarryId.IsUpCarryId() ? MaxUpLength : MaxNpPostBoxLength)}");
        }

        public static PropertyMetadataBuilder<T, decimal> NpPackageInsurance<T>(this PropertyMetadataBuilder<T, decimal> builder, decimal minPackageInsurance = 500m)
        {
            const decimal MaxPackageInsurance = 50000m;

            return builder.MatchesRule(
                x => x >= minPackageInsurance && x <= MaxPackageInsurance,
                () => $"Сумма страховки должна быть в пределах {minPackageInsurance:F0}..{MaxPackageInsurance:F0}");
        }

        public static PropertyMetadataBuilder<T, decimal> MaxProductPrice<T>(this PropertyMetadataBuilder<T, decimal> builder)
        {
            const decimal MaxProductPrice = Constants.MaxProductPrice;

            return builder.MatchesRule(
                x => x <= MaxProductPrice,
                () => $"Цена товара должна быть не больше {MaxProductPrice}");
        }

        public static PropertyMetadataBuilder<T, decimal?> MaxProductPrice<T>(this PropertyMetadataBuilder<T, decimal?> builder)
        {
            const decimal MaxProductPrice = Constants.MaxProductPrice;

            return builder.MatchesRule(
                x => x <= MaxProductPrice,
                () => $"Цена товара должна быть не больше {MaxProductPrice}");
        }

        public static PropertyMetadataBuilder<T, ComboBoxItem?> RequiredActive<T>(this PropertyMetadataBuilder<T, ComboBoxItem?> builder, Func<T, bool> active, string errorMessage = null)
        {
            errorMessage ??= "Поле не заполнено либо заполнено неактивным значением";

            return builder.MatchesInstanceRule((x, y) => x != null && x != default(ComboBoxItem) && (!active(y) || x.Value.Active), () => errorMessage);
        }
    }
}