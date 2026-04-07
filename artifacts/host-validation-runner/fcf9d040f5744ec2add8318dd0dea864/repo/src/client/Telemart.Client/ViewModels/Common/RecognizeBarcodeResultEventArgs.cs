using System;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class RecognizeBarcodeResultEventArgs : EventArgs
    {
        private RecognizeBarcodeResultEventArgs(
            RecognizeBarcodeResult result,
            int? productId,
            ProductAttributesDto product,
            int quantity,
            string barcodeText,
            RecognizeBarcodeMessage message = null)
        {
            Result = result;
            ProductId = productId;
            Product = product;
            Quantity = quantity;
            BarcodeText = barcodeText;
            Message = message;
        }

        private RecognizeBarcodeResultEventArgs(
            RecognizeBarcodeResult result,
            int? assemblyId,
            string barcodeText,
            RecognizeBarcodeMessage message = null,
            int? additionalServiceProductId = null,
            int? productId = null)
        {
            Result = result;
            AssemblyServiceId = assemblyId;
            AdditionalServiceProductId = additionalServiceProductId;
            ProductId = productId;
            BarcodeText = barcodeText;
            Message = message;
        }

        private RecognizeBarcodeResultEventArgs(
            RecognizeBarcodeResult result,
            int? productId,
            ProductAttributesDto product,
            int quantity,
            string barcodeText,
            string parentCategoryName,
            RecognizeBarcodeMessage message = null,
            Action warningAction = null)
            : this(result, productId, product, quantity, barcodeText, message)
        {
            ParentCategoryName = parentCategoryName;
            WarningAction = warningAction;
        }

        public string ParentCategoryName { get; set; }

        public string BarcodeText { get; private set; }

        public ProductAttributesDto Product { get; private set; }

        public int? ProductId { get; private set; }

        public int? AssemblyServiceId { get; private set; }

        public int? AdditionalServiceProductId { get; }

        public int Quantity { get; private set; }

        public RecognizeBarcodeResult Result { get; private set; }

        public RecognizeBarcodeMessage Message { get; set; }

        public Action WarningAction { get; set; }

        public static RecognizeBarcodeResultEventArgs Error(string barcode, string errorText)
        {
            RecognizeBarcodeMessage message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, errorText);
            return new RecognizeBarcodeResultEventArgs(RecognizeBarcodeResult.Error, null, null, 0, barcode, message);
        }

        public static RecognizeBarcodeResultEventArgs Warning(string barcode, string warningText)
        {
            RecognizeBarcodeMessage message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, warningText);
            return new RecognizeBarcodeResultEventArgs(RecognizeBarcodeResult.Error, null, null, 0, barcode, message);
        }

        public static RecognizeBarcodeResultEventArgs Found(int productId, ProductAttributesDto product, int quantity, string barcode, string parentCategoryName)
        {
            return new RecognizeBarcodeResultEventArgs(RecognizeBarcodeResult.Found, productId, product, quantity, barcode, parentCategoryName);
        }

        public static RecognizeBarcodeResultEventArgs FoundAssembly(int assemblyId, string barcode)
        {
            return new RecognizeBarcodeResultEventArgs(RecognizeBarcodeResult.FoundAssembly, assemblyId, barcode);
        }

        public static RecognizeBarcodeResultEventArgs FoundAdditionalService(int additionalServiceProductId, string barcode)
        {
            RecognizeBarcodeResultEventArgs args = new RecognizeBarcodeResultEventArgs(
                RecognizeBarcodeResult.FoundAdditionalServiceProduct,
                null,
                barcode,
                additionalServiceProductId: additionalServiceProductId,
                message: new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Info, $"Распознано услугу: {additionalServiceProductId} ({barcode})"))
            {
                Quantity = 1
            };

            return args;
        }

        public static RecognizeBarcodeResultEventArgs FoundInSupplier(int productId, ProductAttributesDto product, int quantity, string barcode)
        {
            return new RecognizeBarcodeResultEventArgs(RecognizeBarcodeResult.FoundInSupplier, productId, product, quantity, barcode);
        }

        public static RecognizeBarcodeResultEventArgs NotFound(string barcode, int quantity)
        {
            return new RecognizeBarcodeResultEventArgs(RecognizeBarcodeResult.NotFound, null, null, quantity, barcode);
        }
    }
}