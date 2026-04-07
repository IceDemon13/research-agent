using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Barcode;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class RecognizeBarcodeViewModel : TelemartViewModelBase
    {
        private readonly HashSet<string> addedBarcodes = new HashSet<string>();
        private readonly Dictionary<string, int> barcodeDictionary = new Dictionary<string, int>();
        private readonly Dictionary<int, ProductAttributesDto> productDictionary = new Dictionary<int, ProductAttributesDto>();

        private RecognizeBarcodeSettings settings;

        public RecognizeBarcodeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            RecognizeBarcodeCommand = new AsyncCommand<string>(RecognizeBarcodeAsync);

            UseQuantity = true;
            Quantity = 1;
        }

        public RecognizeBarcodeViewModel()
        {
        }

        public event EventHandler OnFinishCommand;

        public event EventHandler<RecognizeBarcodeResultEventArgs> OnFinished;

        public event EventHandler OnStarted;

        #region Commands

        public IAsyncCommand RecognizeBarcodeCommand { get; }

        #endregion

        #region INPC

        public string BarcodeText
        {
            get { return GetProperty(() => BarcodeText); }
            set { SetProperty(() => BarcodeText, value); }
        }

        public decimal Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public bool UseQuantity
        {
            get { return GetProperty(() => UseQuantity); }
            set { SetProperty(() => UseQuantity, value); }
        }

        public ObservableRangeCollection<BarcodeRecognitionViewItem> RecognitionViewItems
        {
            get { return GetProperty(() => RecognitionViewItems); }
            set { SetProperty(() => RecognitionViewItems, value); }
        }

        public BarcodeRecognitionViewItem SelectedRecognitionItem
        {
            get { return GetProperty(() => SelectedRecognitionItem); }
            set { SetProperty(() => SelectedRecognitionItem, value); }
        }

        public bool BarcodeFocused
        {
            get { return GetProperty(() => BarcodeFocused); }
            set { SetProperty(() => BarcodeFocused, value); }
        }

        #endregion

        public void AddBarcodeAssignment(string barcode, int productId)
        {
            addedBarcodes.Add(barcode);
            barcodeDictionary.Add(barcode, productId);
        }

        public void AddProduct(ProductAttributesDto product)
        {
            productDictionary[product.ProductId] = product;

            if (product.Barcodes != null)
            {
                foreach (string barcode in product.Barcodes.Select(x => x.Barcode))
                {
                    barcodeDictionary[barcode] = product.ProductId;
                }
            }
        }

        public ProductAttributesDto FindByBarcode(string barcode)
        {
            ProductAttributesDto product = null;

            if (barcodeDictionary.TryGetValue(barcode, out int productId))
            {
                if (!productDictionary[productId].SelfBarcode)
                {
                    product = productDictionary[productId];
                }
            }

            return product;
        }

        public ProductAttributesDto FindById(int productId)
        {
            productDictionary.TryGetValue(productId, out ProductAttributesDto product);
            return product;
        }

        public ProductAttributesDto FindBySupplierBarcode(string barcodeText)
        {
            return productDictionary.Values.FirstOrDefault(x => x.CompetitorsBarcodes.Contains(barcodeText));
        }

        public IEnumerable<string> GetAssignedBarcodesById(int productId)
        {
            return barcodeDictionary
                .Where(pair => addedBarcodes.Contains(pair.Key) && pair.Value == productId)
                .Select(pair => pair.Key);
        }

        public IEnumerable<string> GetBarcodesById(int productId)
        {
            return barcodeDictionary.Where(pair => pair.Value == productId).Select(pair => pair.Key);
        }

        public void Init(RecognizeBarcodeSettings recognizeBarcodeSettings, IReadOnlyCollection<ProductAttributesDto> products)
        {
            settings = recognizeBarcodeSettings;
            products.ForEach(AddProduct);
            RecognitionViewItems = new ObservableRangeCollection<BarcodeRecognitionViewItem>();
        }

        public void RemoveAssignedBarcodes(int productId)
        {
            for (int i = addedBarcodes.Count - 1; i >= 0; i--)
            {
                string addedBarcode = addedBarcodes.ElementAt(i);

                if (barcodeDictionary[addedBarcode] == productId)
                {
                    addedBarcodes.Remove(addedBarcode);
                    barcodeDictionary.Remove(addedBarcode);
                }
            }
        }

        public void Update(ProductCardDto productCard)
        {
            if (productDictionary.TryGetValue(productCard.ProductId, out ProductAttributesDto product))
            {
                HashSet<string> barcodeHashSet = new HashSet<string>(productCard.Barcodes.Select(x => x.Barcode), StringComparer.OrdinalIgnoreCase);

                product.KeepSerial = productCard.KeepSerial;
                product.SerialNumberLength = productCard.SerialNumberLength.ToList();
                product.SelfBarcode = productCard.SelfBarcode;

                product.Depth = productCard.Depth;
                product.Height = productCard.Height;
                product.Weight = productCard.Weight;

                product.Barcodes = productCard.Barcodes.DistinctBy(x => x.Barcode, StringComparer.OrdinalIgnoreCase).ToList();
                product.CompetitorsBarcodes = new List<string>(product.CompetitorsBarcodes);

                if (product.SelfBarcode)
                {
                    RemoveAssignedBarcodes(product.ProductId);
                }

                for (int i = barcodeDictionary.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<string, int> pair = barcodeDictionary.ElementAt(i);

                    if (pair.Value == product.ProductId && !barcodeHashSet.Contains(pair.Key) && !addedBarcodes.Contains(pair.Key))
                    {
                        barcodeDictionary.Remove(pair.Key);
                    }
                }
            }
        }

        public void SetFocusOnBarcode()
        {
            BarcodeFocused = true;
            RaisePropertyChanged(nameof(BarcodeFocused));
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            UseQuantity = true;
            Quantity = 1;
        }

        private void FireOnFinished(RecognizeBarcodeResultEventArgs args)
        {
            OnFinished?.Invoke(this, args);

            BarcodeRecognitionViewItem recognitionItem = null;

            if (args.Message != null)
            {
                recognitionItem = new BarcodeRecognitionViewItem
                {
                    Message = args.Message.MessageText,
                    Result = args.Message.Type,
                    Time = DateTime.Now,
                    ActionWarning = args.WarningAction
                };
            }
            else
            {
                switch (args.Result)
                {
                    case RecognizeBarcodeResult.Found:
                    case RecognizeBarcodeResult.FoundInSupplier:
                        recognitionItem = new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Info, $"Распознано: {args.Product.Name} ({args.BarcodeText})", DateTime.Now);
                        break;

                    case RecognizeBarcodeResult.FoundAssembly:
                        recognitionItem = new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Info, $"Распознано сборку: {args.AssemblyServiceId} ({args.BarcodeText})", DateTime.Now);
                        break;

                    case RecognizeBarcodeResult.Error:
                        recognitionItem = new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Error, $"Ошибка при распознавании ШК {args.BarcodeText}", DateTime.Now);
                        break;

                    case RecognizeBarcodeResult.NotFound:
                        recognitionItem = new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Error, $"Товар не найден ({args.BarcodeText})", DateTime.Now);
                        break;
                }
            }

            if (recognitionItem != null)
            {
                switch (recognitionItem.Result)
                {
                    case RecognizeBarcodeMessageType.Error:
                        MessageFacadeService.ShowNotificationError(recognitionItem.Message, true);
                        break;
                    case RecognizeBarcodeMessageType.Warning:
                        MessageFacadeService.ShowNotificationWarning(recognitionItem.Message, true);
                        recognitionItem.ActionWarning?.Invoke();
                        break;
                }

                RecognitionViewItems.Insert(0, recognitionItem);
                SelectedRecognitionItem = recognitionItem;
            }
        }

        private void FireOnStarted()
        {
            OnStarted?.Invoke(this, EventArgs.Empty);
        }

        private async Task RecognizeBarcodeAsync(string barcodeText)
        {
            if (string.IsNullOrEmpty(barcodeText))
            {
                return;
            }

            try
            {
                if (string.Equals(barcodeText, BarcodeConstants.CmdFinish, StringComparison.Ordinal))
                {
                    OnFinishCommand?.Invoke(this, EventArgs.Empty);
                    return;
                }

                FireOnStarted();

                barcodeText = Regex.IsMatch(barcodeText, @"^\d{12}$")
                    ? $"0{barcodeText}"
                    : barcodeText;

                Code39Barcode code39Barcode = new(barcodeText);
                Ean13Barcode ean13Barcode = new(barcodeText);
                OurBarcode ourBarcode = new(barcodeText);
                OurAssemblyServiceBarcode ourAssemblyServiceBarcode = new(barcodeText);
                OurAdditionalServiceBarcode ourAdditionalServiceBarcode = new(barcodeText);

                if (string.IsNullOrEmpty(barcodeText)
                    || (!(settings.AllowOur && ourBarcode.IsValid)
                        && !(settings.AllowOurAssemblyService && ourAssemblyServiceBarcode.IsValid)
                        && !(settings.AllowOurAdditionalService && ourAdditionalServiceBarcode.IsValid)
                        && (barcodeText.Length < settings.MinLength || barcodeText.Length > settings.MaxLength)))
                {
                    FireOnFinished(RecognizeBarcodeResultEventArgs.Warning(barcodeText, $"Длина ШК должна быть от {settings.MinLength} до {settings.MaxLength}"));
                    return;
                }

                bool valid = (settings.AllowCode39 && code39Barcode.IsValid)
                    || (settings.AllowEan13 && ean13Barcode.IsValid)
                    || (settings.AllowOur && ourBarcode.IsValid)
                    || (settings.AllowOurAdditionalService && ourAdditionalServiceBarcode.IsValid)
                    || (settings.AllowOurAssemblyService && ourAssemblyServiceBarcode.IsValid);

                if (!valid)
                {
                    FireOnFinished(RecognizeBarcodeResultEventArgs.Warning(barcodeText, "Неверный ШК"));
                    return;
                }

                ProductAttributesDto product;
                int quantity = (int)Quantity;

                if (settings.AllowOur && ourBarcode.IsValid)
                {
                    quantity = ourBarcode.Quantity > 1
                        ? ourBarcode.Quantity
                        : quantity;

                    product = FindById(ourBarcode.ProductId);
                }
                else if (settings.AllowOurAssemblyService && ourAssemblyServiceBarcode.IsValid)
                {
                    FireOnFinished(RecognizeBarcodeResultEventArgs.FoundAssembly(ourAssemblyServiceBarcode.AssemblyServiceId, barcodeText));
                    return;
                }
                else if (settings.AllowOurAdditionalService && ourAdditionalServiceBarcode.IsValid)
                {
                    if (settings.AdditionalServiceProductIds.Any(x => x == ourAdditionalServiceBarcode.AdditionalServiceProductId))
                    {
                        FireOnFinished(RecognizeBarcodeResultEventArgs.FoundAdditionalService(ourAdditionalServiceBarcode.AdditionalServiceProductId, barcodeText));
                    }
                    else
                    {
                        FireOnFinished(RecognizeBarcodeResultEventArgs.Error(barcodeText, "Услуга не найдена"));
                    }

                    return;
                }
                else
                {
                    product = FindByBarcode(barcodeText);
                }

                if (product != null)
                {
                    FireOnFinished(
                        RecognizeBarcodeResultEventArgs.Found(
                            product.ProductId,
                            product,
                            quantity,
                            barcodeText,
                            product.ParentCategoryName));

                    return;
                }

                product = await WebClient.ExecuteApiRequestAsync(new QueryProductAttributesByBarcode(barcodeText, settings.IncludeSn));

                if (product != null)
                {
                    AddProduct(product);

                    FireOnFinished(
                        RecognizeBarcodeResultEventArgs.Found(
                            product.ProductId,
                            product,
                            quantity,
                            barcodeText,
                            product.ParentCategoryName));

                    return;
                }

                product = FindBySupplierBarcode(barcodeText);

                if (product != null)
                {
                    FireOnFinished(
                        RecognizeBarcodeResultEventArgs.FoundInSupplier(
                            product.ProductId,
                            product,
                            quantity,
                            barcodeText));

                    return;
                }

                FireOnFinished(RecognizeBarcodeResultEventArgs.NotFound(barcodeText, quantity));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to recognize product by barcode");
                FireOnFinished(RecognizeBarcodeResultEventArgs.Error(barcodeText, "Ошибка при распознавании ШК"));
            }
            finally
            {
                BarcodeText = string.Empty;
                Quantity = 1;
            }
        }
    }
}