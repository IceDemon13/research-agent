using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace Telemart.Client.Business
{
    public sealed class ProductBarcode
    {
        private const string BarcodeValidationRegex = @"^[A-Z0-9-]*$";
        private const int MaxBarcodeLength = 20;
        private const int MinBarcodeLength = 13;
        private const string OurBarcodeValidationRegex = @"^TEL-[1-9]+[0-9]*$";

        public ProductBarcode(string barcode)
        {
            if (barcode != null && Regex.IsMatch(barcode, OurBarcodeValidationRegex))
            {
                IsOur = true;
                OurProductId = int.Parse(barcode.Substring(4));

                Barcode = barcode;
                IsValid = true;
                Errors = new List<ValidationResult>();
            }
            else
            {
                IsOur = false;
                OurProductId = null;

                Barcode = barcode?.Length == 12
                    ? $"0{barcode}"
                    : barcode;

                Errors = ValidateProductBarcode(Barcode).ToList();
                IsValid = !Errors.Any();
            }
        }

        public string Barcode { get; }

        public IReadOnlyCollection<ValidationResult> Errors { get; }

        public bool IsOur { get; }

        public bool IsValid { get; }

        public int? OurProductId { get; }

        public override string ToString()
        {
            return Barcode;
        }

        private static IEnumerable<ValidationResult> ValidateProductBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode) || barcode.Length < MinBarcodeLength || barcode.Length > MaxBarcodeLength)
            {
                yield return new ValidationResult($"Длина ШК должна быть от {MinBarcodeLength} до {MaxBarcodeLength} символов");
            }

            if (!string.IsNullOrEmpty(barcode) && !Regex.IsMatch(barcode, BarcodeValidationRegex))
            {
                yield return new ValidationResult("Неверный ШК");
            }
        }
    }
}