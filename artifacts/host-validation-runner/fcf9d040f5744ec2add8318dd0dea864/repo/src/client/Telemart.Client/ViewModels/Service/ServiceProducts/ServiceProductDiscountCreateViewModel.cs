using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using SmartFormat;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.ServiceProduct;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Content.ProductImages;
using Telemart.Common.ErrorHandling;
using IFileInfo = System.IO.Abstractions.IFileInfo;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductDiscountCreateViewModel : TelemartDialogViewModelBase
    {
        private const int BadPixelDiscountProductDefect = 4;

        private int productId;
        private int serviceProductTypeId;
        private string baseFilePath;

        private ServiceProductDiscountParameter _parameter;
        private ProductDiscountCreateDto _productDiscountCreateDto;

        public ServiceProductDiscountCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IFileSystem fileSystem,
            IProductImageFileValidator productImageFileValidator,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IPrintingSettingsStore printingSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            ProductImageFileValidator = productImageFileValidator ?? throw new ArgumentNullException(nameof(productImageFileValidator));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            ErrorHandler = errorHandler;
            PrintingSettingsStore = printingSettingsStore;

            SelectPhotosPathCommand = new DelegateCommand(SelectPhotosPath);
            DeleteSelectPhotosCommand = new DelegateCommand(DeletrSelectPhotosPath);
        }

        public ServiceProductDiscountCreateViewModel()
        {
        }

        #region Commands

        public IDelegateCommand SelectPhotosPathCommand { get; }

        public IDelegateCommand DeleteSelectPhotosCommand { get; }

        #endregion

        #region INPC

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
        }

        public string ProductNameUkr
        {
            get { return GetProperty(() => ProductNameUkr); }
            private set { SetProperty(() => ProductNameUkr, value); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            private set { SetProperty(() => ProductNameEn, value); }
        }

        public string Defect
        {
            get { return GetProperty(() => Defect); }
            set { SetProperty(() => Defect, value, OnDefectChanged); }
        }

        public string DefectUkr
        {
            get { return GetProperty(() => DefectUkr); }
            set { SetProperty(() => DefectUkr, value, OnDefectUkrChanged); }
        }

        public string DefectEn
        {
            get { return GetProperty(() => DefectEn); }
            set { SetProperty(() => DefectEn, value, OnDefectEnChanged); }
        }

        public string DefectDescription
        {
            get { return GetProperty(() => DefectDescription); }
            set { SetProperty(() => DefectDescription, value); }
        }

        public string DefectDescriptionUkr
        {
            get { return GetProperty(() => DefectDescriptionUkr); }
            set { SetProperty(() => DefectDescriptionUkr, value); }
        }

        public string DefectDescriptionEn
        {
            get { return GetProperty(() => DefectDescriptionEn); }
            set { SetProperty(() => DefectDescriptionEn, value); }
        }

        public string DiscountProductName
        {
            get { return GetProperty(() => DiscountProductName); }
            set { SetProperty(() => DiscountProductName, value); }
        }

        public string DiscountProductNameUkr
        {
            get { return GetProperty(() => DiscountProductNameUkr); }
            set { SetProperty(() => DiscountProductNameUkr, value); }
        }

        public string DiscountProductNameEn
        {
            get { return GetProperty(() => DiscountProductNameEn); }
            set { SetProperty(() => DiscountProductNameEn, value); }
        }

        public Warranty WarrantyRetail
        {
            get { return GetProperty(() => WarrantyRetail); }
            set { SetProperty(() => WarrantyRetail, value); }
        }

        public Warranty WarrantyWholesale
        {
            get { return GetProperty(() => WarrantyWholesale); }
            set { SetProperty(() => WarrantyWholesale, value); }
        }

        public string ImagesPath
        {
            get { return GetProperty(() => ImagesPath); }
            set { SetProperty(() => ImagesPath, value); }
        }

        public ReadOnlyObservableCollection<DiscountProductPrefixDto> DiscountProductPrefixes
        {
            get { return GetProperty(() => DiscountProductPrefixes); }
            private set { SetProperty(() => DiscountProductPrefixes, value); }
        }

        public ReadOnlyObservableCollection<DiscountProductDefectDto> DiscountProductDefects
        {
            get { return GetProperty(() => DiscountProductDefects); }
            private set { SetProperty(() => DiscountProductDefects, value); }
        }

        public DiscountProductPrefixDto SelectedDiscountProductPrefix
        {
            get { return GetProperty(() => SelectedDiscountProductPrefix); }
            set { SetProperty(() => SelectedDiscountProductPrefix, value, SelectedDiscountProductPrefixChanged); }
        }

        public DiscountProductDefectDto SelectedDiscountProductDefect
        {
            get { return GetProperty(() => SelectedDiscountProductDefect); }
            set { SetProperty(() => SelectedDiscountProductDefect, value, SelectedDiscountProductDefectChanged); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<Warranty> Warranties
        {
            get { return GetProperty(() => Warranties); }
            private set { SetProperty(() => Warranties, value); }
        }

        public WarehouseDto SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public ProductAttributesDto ProductAttributes
        {
            get { return GetProperty(() => ProductAttributes); }
            private set { SetProperty(() => ProductAttributes, value); }
        }

        public int QuantityBadPixel
        {
            get { return GetProperty(() => QuantityBadPixel); }
            set { SetProperty(() => QuantityBadPixel, value, QuantityBadPixelChanged); }
        }

        public bool UseNewProductPhoto
        {
            get { return GetProperty(() => UseNewProductPhoto); }
            set { SetProperty(() => UseNewProductPhoto, value, UseNewProductPhotoChanged); }
        }

        public bool WarehouseIsReadOnly
        {
            get { return GetProperty(() => WarehouseIsReadOnly); }
            private set { SetProperty(() => WarehouseIsReadOnly, value); }
        }

        public bool UseNewProductPhotoIsReadOnly
        {
            get { return GetProperty(() => UseNewProductPhotoIsReadOnly); }
            private set { SetProperty(() => UseNewProductPhotoIsReadOnly, value); }
        }

        public bool QuantityBadPixelVisible => SelectedDiscountProductDefect?.Id == BadPixelDiscountProductDefect;

        #endregion

        private IFileSystem FileSystem { get; }

        private IProductImageFileValidator ProductImageFileValidator { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IFolderBrowserDialogService FolderBrowserDialogService => GetService<IFolderBrowserDialogService>();

        public static void BuildMetadata(MetadataBuilder<ServiceProductDiscountCreateViewModel> builder)
        {
            builder.Property(x => x.Defect)
                .MinLength(5, () => "Минимальная длина 5 символов")
                .MaxLength(100, () => "Максимальная длина 100 символов")
                .MatchesInstanceRule((x, _) => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.DefectUkr)
                .MinLength(5, () => "Минимальная длина 5 символов")
                .MaxLength(100, () => "Максимальная длина 100 символов")
                .MatchesInstanceRule((x, _) => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.DefectEn)
                .MinLength(5, () => "Минимальная длина 5 символов")
                .MaxLength(100, () => "Максимальная длина 100 символов")
                .MatchesInstanceRule((x, _) => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedDiscountProductPrefix).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DefectDescription).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DefectDescriptionUkr).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DefectDescriptionEn).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarrantyRetail).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarrantyWholesale).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedWarehouse).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ImagesPath)
                .MatchesInstanceRule((x, y) => !string.IsNullOrWhiteSpace(x) || y.UseNewProductPhoto, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.QuantityBadPixel).MatchesInstanceRule(
                (x, y) => y.SelectedDiscountProductDefect?.Id != 4 || (x > 0 && x <= 100000000),
                () => "Допустимое значение 1….100000000.");
        }

        public ProductDiscountCreateDto GetProductDiscount()
        {
            return _productDiscountCreateDto;
        }

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (ServiceProductDiscountParameter)Parameter;

            productId = _parameter.ProductId;
            serviceProductTypeId = _parameter.ServiceProductTypeId;

            (List<DiscountProductDefectDto> discountProductDefects,
                List<DiscountProductPrefixDto> discountProductPrefixes,
                PagedResult<WarehouseDto> warehouses,
                PrintingSettingsInfo loadImagesPath) = await TaskExt.WhenAll(
                    WebClient.ExecuteApiRequestAsync(new QueryDiscountProductDefects()),
                    WebClient.ExecuteApiRequestAsync(new QueryDiscountProductPrefixes()),
                    WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true),
                    PrintingSettingsStore.LoadAsync());

            ServiceRequestDto serviceRequest = null;

            if (_parameter.ServiceRequestId.HasValue)
            {
                serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(_parameter.ServiceRequestId.Value));
            }

            Warehouses = warehouses.Data
                .Where(x => (x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id)) || x.Id == _parameter.WarehouseId)
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            Warranties = Dictionaries.GetItems<Warranty>()
                .OrderBy(x => x.Weight)
                .ToReadOnlyObservableCollection();

            ProductAttributes = await WebClient.ExecuteApiRequestAsync(new QueryProductAttributes(_parameter.ProductId));

            MapProductAttributes(ProductAttributes);

            if (serviceRequest?.Requirement == ServiceRequestRequirement.TradeInId)
            {
                Warranty defaultWarranty = Warranties.FirstOrDefault(x => x.TradeInDefault);

                WarrantyRetail = defaultWarranty ?? WarrantyRetail;
                WarrantyWholesale = defaultWarranty ?? WarrantyWholesale;
            }

            UseNewProductPhotoIsReadOnly = !ProductAttributes.AnyImages;

            UseNewProductPhoto = _parameter.DefectCreateDiscount;

            DiscountProductPrefixes = discountProductPrefixes
                .Where(x => (serviceProductTypeId == ServiceProductType.TradeIn.Id) == x.TradeIn)
                .ToReadOnlyObservableCollection();

            DiscountProductDefects = discountProductDefects
                .Where(x => (serviceProductTypeId == ServiceProductType.TradeIn.Id) == x.TradeIn)
                .ToReadOnlyObservableCollection();

            SelectedDiscountProductPrefix = DiscountProductPrefixes.FirstOrDefault();

            SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == _parameter.WarehouseId);
            WarehouseIsReadOnly = true;
            baseFilePath = loadImagesPath.FilesPath;

            Title = "Создание уценки";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (!string.IsNullOrEmpty(ImagesPath) && !Directory.Exists(ImagesPath))
            {
                MessageFacadeService.ShowNotificationWarning($"Такой путь {ImagesPath} не существует");
                return;
            }

            IReadOnlyCollection<ImageDto> images = null;

            if (!string.IsNullOrWhiteSpace(ImagesPath))
            {
                images = await GetImagesAsync(ImagesPath);

                if (images == null || !images.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("В папке нет фото в нужном формате");
                    return;
                }
            }

            _productDiscountCreateDto = new ProductDiscountCreateDto
            {
                DefectId = SelectedDiscountProductDefect?.Id,
                Defect = $"{Defect}{(QuantityBadPixel > 0 ? $", {QuantityBadPixel.ToString()}шт." : string.Empty)}",
                DefectUkr = $"{DefectUkr}{(QuantityBadPixel > 0 ? $", {QuantityBadPixel.ToString()}шт." : string.Empty)}",
                DefectEn = $"{DefectEn}{(QuantityBadPixel > 0 ? $", {QuantityBadPixel.ToString()}pcs." : string.Empty)}",
                DefectDescription = DefectDescription,
                DefectDescriptionUkr = DefectDescriptionUkr,
                DefectDescriptionEn = DefectDescriptionEn,
                Images = images,
                UseNewProductPhoto = UseNewProductPhoto,
                WarrantyRetailId = WarrantyRetail.Id,
                WarrantyWholesaleId = WarrantyWholesale.Id,
                ServiceProductTypeId = serviceProductTypeId,
                DiscountProductPrefixId = SelectedDiscountProductPrefix.Id,
                DefectCreateDiscount = _parameter.DefectCreateDiscount,
                BadPixelQuantity = QuantityBadPixel
            };

            if (_parameter.DefectCreateDiscount == false)
            {
                await CreateProductDiscountAsync(_productDiscountCreateDto);
            }

            IsOk = true;
            Close();
        }

        private async Task CreateProductDiscountAsync(ProductDiscountCreateDto dto)
        {
            Result<ProductCardDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateProductDiscount(productId, dto)),
                "создании уцененного товара",
                "Уцененный товар",
                this,
                true);

            if (result.IsSuccess)
            {
                Messenger.Send(new DiscountProductMessage(result.Data, MessageType.Added));
            }
        }

        private async Task<IReadOnlyCollection<ImageDto>> GetImagesAsync(string directoryPath)
        {
            List<ImageDto> images = new List<ImageDto>();

            IEnumerable<IFileInfo> files = FileSystem.DirectoryInfo
                .New(directoryPath)
                .EnumerateFiles()
                .Where(x => !ProductImageFileValidator.ValidateFile(x, false).Any());

            foreach (IFileInfo file in files)
            {
                byte[] bytes = await FileHelper.ReadBytesAsync(file.FullName);
                images.Add(new ImageDto(file.Name, bytes));
            }

            return images;
        }

        private void OnDefectChanged()
        {
            if (string.IsNullOrEmpty(Defect))
            {
                DiscountProductName = string.Empty;
                return;
            }

            if (SelectedDiscountProductDefect.Id != BadPixelDiscountProductDefect)
            {
                QuantityBadPixel = 0;
            }

            string badPixel = QuantityBadPixel > 0 ? $", {QuantityBadPixel.ToString()}шт." : string.Empty;

            DiscountProductName = $"{SelectedDiscountProductPrefix?.Prefix} {ProductName} ({Defect}{badPixel}{(serviceProductTypeId == ServiceProductType.TradeIn.Id ? $", {productId}" : string.Empty)})"
                .TrimStart()
                .ToUpperFirstLetter();
        }

        private void OnDefectUkrChanged()
        {
            if (string.IsNullOrEmpty(DefectUkr))
            {
                DiscountProductNameUkr = string.Empty;
                return;
            }

            if (SelectedDiscountProductDefect.Id != BadPixelDiscountProductDefect)
            {
                QuantityBadPixel = 0;
            }

            string badPixel = QuantityBadPixel > 0 ? $", {QuantityBadPixel.ToString()}шт." : string.Empty;

            DiscountProductNameUkr = $"{SelectedDiscountProductPrefix?.PrefixUkr} {ProductNameUkr} ({DefectUkr}{badPixel}{(serviceProductTypeId == ServiceProductType.TradeIn.Id ? $", {productId}" : string.Empty)})"
                .TrimStart()
                .ToUpperFirstLetter();
        }

        private void OnDefectEnChanged()
        {
            if (string.IsNullOrEmpty(DefectEn))
            {
                DiscountProductNameEn = string.Empty;
                return;
            }

            if (SelectedDiscountProductDefect.Id != BadPixelDiscountProductDefect)
            {
                QuantityBadPixel = 0;
            }

            string badPixel = QuantityBadPixel > 0 ? $", {QuantityBadPixel.ToString()}pcs." : string.Empty;

            DiscountProductNameEn = $"{SelectedDiscountProductPrefix?.PrefixEn} {ProductNameEn} ({DefectEn}{badPixel}{(serviceProductTypeId == ServiceProductType.TradeIn.Id ? $", {productId}" : string.Empty)})"
                .TrimStart()
                .ToUpperFirstLetter();
        }

        private void MapProductAttributes(ProductAttributesDto product)
        {
            ProductName = product.FullName;
            ProductNameUkr = product.FullNameUa;
            ProductNameEn = product.FullNameEn;
            WarrantyRetail = Warranties.FirstOrDefault(x => x.Id == product.WarrantyRetailId);
            WarrantyWholesale = Warranties.FirstOrDefault(x => x.Id == product.WarrantyWholesaleId);
        }

        private void SelectPhotosPath()
        {
            FolderBrowserDialogService.StartPath = baseFilePath;

            bool isOk = FolderBrowserDialogService.ShowDialog();

            if (isOk)
            {
                ImagesPath = FolderBrowserDialogService.ResultPath;
            }
        }

        private void DeletrSelectPhotosPath()
        {
            ImagesPath = string.Empty;
        }

        private void UseNewProductPhotoChanged()
        {
            if (UseNewProductPhoto)
            {
                DeletrSelectPhotosPath();
            }

            RaisePropertyChanged(nameof(ImagesPath));
        }

        private void SelectedDiscountProductPrefixChanged()
        {
            OnDefectChanged();
            OnDefectUkrChanged();
            OnDefectEnChanged();
        }

        private void SelectedDiscountProductDefectChanged()
        {
            if (SelectedDiscountProductDefect is null)
            {
                return;
            }

            Defect = SelectedDiscountProductDefect.Defect;
            DefectUkr = SelectedDiscountProductDefect.DefectUkr;
            DefectEn = SelectedDiscountProductDefect.DefectEn;

            DefectDescription = string.Empty;
            DefectDescriptionUkr = string.Empty;
            DefectDescriptionEn = string.Empty;

            if (SelectedDiscountProductDefect.Id == BadPixelDiscountProductDefect)
            {
                QuantityBadPixelChanged();
            }
            else
            {
                QuantityBadPixel = 0;

                DefectDescription = SelectedDiscountProductDefect.DescriptionTemplate ?? string.Empty;
                DefectDescriptionUkr = SelectedDiscountProductDefect.DescriptionTemplateUkr ?? string.Empty;
                DefectDescriptionEn = SelectedDiscountProductDefect.DescriptionTemplateEn ?? string.Empty;
            }

            RaisePropertiesChanged(nameof(QuantityBadPixelVisible), nameof(QuantityBadPixel));
        }

        private void QuantityBadPixelChanged()
        {
            if (SelectedDiscountProductDefect.DescriptionTemplate is null)
            {
                return;
            }

            object data = new { BadPixel = QuantityBadPixel };

            DefectDescription = Smart.Format(SelectedDiscountProductDefect.DescriptionTemplate, data);
            DefectDescriptionUkr = Smart.Format(SelectedDiscountProductDefect.DescriptionTemplateUkr, data);
            DefectDescriptionEn = Smart.Format(SelectedDiscountProductDefect.DescriptionTemplateEn, data);

            OnDefectChanged();
            OnDefectUkrChanged();
            OnDefectEnChanged();
        }
    }
}