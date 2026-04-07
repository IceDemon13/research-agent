using System;
using System.Text.RegularExpressions;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Validation;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Constants;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInViewItem : TelemartEditorViewItemBase, ILocalіzableEntity
    {
        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value, () => RaisePropertiesChanged(nameof(OrderFound))); }
        }

        public int? PresaleOrderId
        {
            get { return GetProperty(() => PresaleOrderId); }
            set { SetProperty(() => PresaleOrderId, value); }
        }

        public int? OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int? TradeInSegmentId
        {
            get { return GetProperty(() => TradeInSegmentId); }
            set { SetProperty(() => TradeInSegmentId, value); }
        }

        public string TradeInSegmentName
        {
            get { return GetProperty(() => TradeInSegmentName); }
            set { SetProperty(() => TradeInSegmentName, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public int? CustomerId
        {
            get { return GetProperty(() => CustomerId); }
            set { SetProperty(() => CustomerId, value); }
        }

        public int? BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime? EvaluatedOn
        {
            get { return GetProperty(() => EvaluatedOn); }
            set { SetProperty(() => EvaluatedOn, value, () => RaisePropertyChanged(nameof(RedMarkerForEvaluatedOn))); }
        }

        public int? EvaluatedBy
        {
            get { return GetProperty(() => EvaluatedBy); }
            set { SetProperty(() => EvaluatedBy, value); }
        }

        public int? ReceivedBy
        {
            get { return GetProperty(() => ReceivedBy); }
            set { SetProperty(() => ReceivedBy, value); }
        }

        public DateTime? ReceivedOn
        {
            get { return GetProperty(() => ReceivedOn); }
            set { SetProperty(() => ReceivedOn, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string LastName
        {
            get { return GetProperty(() => LastName); }
            set { SetProperty(() => LastName, value); }
        }

        public string FirstName
        {
            get { return GetProperty(() => FirstName); }
            set { SetProperty(() => FirstName, value); }
        }

        public string MiddleName
        {
            get { return GetProperty(() => MiddleName); }
            set { SetProperty(() => MiddleName, value); }
        }

        public string Inn
        {
            get { return GetProperty(() => Inn); }
            set { SetProperty(() => Inn, value); }
        }

        public decimal? MilitaryTaxAmount
        {
            get { return GetProperty(() => MilitaryTaxAmount); }
            set { SetProperty(() => MilitaryTaxAmount, value); }
        }

        public decimal? MilitaryTaxValue
        {
            get { return GetProperty(() => MilitaryTaxValue); }
            set { SetProperty(() => MilitaryTaxValue, value); }
        }

        public decimal? PdfTaxAmount
        {
            get { return GetProperty(() => PdfTaxAmount); }
            set { SetProperty(() => PdfTaxAmount, value); }
        }

        public string Fio => $"{LastName} {FirstName} {MiddleName}";

        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int ClassId
        {
            get { return GetProperty(() => ClassId); }
            set { SetProperty(() => ClassId, value, () => RaisePropertyChanged(nameof(CustomerClassChanged))); }
        }

        public int PackId
        {
            get { return GetProperty(() => PackId); }
            set { SetProperty(() => PackId, value, () => RaisePropertyChanged(nameof(CustomerPackChanged))); }
        }

        public int WarrantyId
        {
            get { return GetProperty(() => WarrantyId); }
            set { SetProperty(() => WarrantyId, value, () => RaisePropertyChanged(nameof(CustomerWarrantyChanged))); }
        }

        public string Brand
        {
            get { return GetProperty(() => Brand); }
            set { SetProperty(() => Brand, value, () => RaisePropertyChanged(nameof(CustomerBrandChanged))); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public int? ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public long? CreateInvoice1cId
        {
            get { return GetProperty(() => CreateInvoice1cId); }
            set { SetProperty(() => CreateInvoice1cId, value); }
        }

        public string CreateInvoice1cResponse
        {
            get { return GetProperty(() => CreateInvoice1cResponse); }
            set { SetProperty(() => CreateInvoice1cResponse, value); }
        }

        public string ReturnInvoice1cResponse
        {
            get { return GetProperty(() => ReturnInvoice1cResponse); }
            set { SetProperty(() => ReturnInvoice1cResponse, value); }
        }

        public string ModelOrPn
        {
            get { return GetProperty(() => ModelOrPn); }
            set { SetProperty(() => ModelOrPn, value, () => RaisePropertyChanged(nameof(CustomerModelOrPnChanged))); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public decimal? BuyoutAmount
        {
            get { return GetProperty(() => BuyoutAmount); }
            set { SetProperty(() => BuyoutAmount, value, () => RaisePropertyChanged(nameof(BuyoutAmountString))); }
        }

        public decimal? RealBuyoutAmount
        {
            get { return GetProperty(() => RealBuyoutAmount); }
            set { SetProperty(() => RealBuyoutAmount, value, () => RaisePropertyChanged(nameof(RealBuyoutAmountString))); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value, () => RaisePropertiesChanged(nameof(AllowEdit), nameof(OrderFound))); }
        }

        public bool Tested
        {
            get { return GetProperty(() => Tested); }
            set { SetProperty(() => Tested, value); }
        }

        public DateTime? TestedOn
        {
            get { return GetProperty(() => TestedOn); }
            set { SetProperty(() => TestedOn, value); }
        }

        public int? TestedBy
        {
            get { return GetProperty(() => TestedBy); }
            set { SetProperty(() => TestedBy, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool ReadyForComplete
        {
            get { return GetProperty(() => ReadyForComplete); }
            set { SetProperty(() => ReadyForComplete, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public bool BoughtInTelemart
        {
            get { return GetProperty(() => BoughtInTelemart); }
            set { SetProperty(() => BoughtInTelemart, value, () => RaisePropertiesChanged(nameof(OrderId), nameof(ProductId))); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string CustomerDescription
        {
            get { return GetProperty(() => CustomerDescription); }
            set { SetProperty(() => CustomerDescription, value); }
        }

        public string CustomerBrand
        {
            get { return GetProperty(() => CustomerBrand); }
            set { SetProperty(() => CustomerBrand, value); }
        }

        public string CustomerModelOrPn
        {
            get { return GetProperty(() => CustomerModelOrPn); }
            set { SetProperty(() => CustomerModelOrPn, value); }
        }

        public int CustomerClassId
        {
            get { return GetProperty(() => CustomerClassId); }
            set { SetProperty(() => CustomerClassId, value); }
        }

        public string ClassDescription
        {
            get { return GetProperty(() => ClassDescription); }
            set { SetProperty(() => ClassDescription, value); }
        }

        public string ClassDescriptionUkr
        {
            get { return GetProperty(() => ClassDescriptionUkr); }
            set { SetProperty(() => ClassDescriptionUkr, value); }
        }

        public int CustomerWarrantyId
        {
            get { return GetProperty(() => CustomerWarrantyId); }
            set { SetProperty(() => CustomerWarrantyId, value); }
        }

        public int CustomerPackId
        {
            get { return GetProperty(() => CustomerPackId); }
            set { SetProperty(() => CustomerPackId, value); }
        }

        public string CustomerComment
        {
            get { return GetProperty(() => CustomerComment); }
            set { SetProperty(() => CustomerComment, value); }
        }

        public decimal? TradeInMaxPrice
        {
            get { return GetProperty(() => TradeInMaxPrice); }
            set { SetProperty(() => TradeInMaxPrice, value); }
        }

        public DateTime? LastDateTradeInMaxPrice
        {
            get { return GetProperty(() => LastDateTradeInMaxPrice); }
            set { SetProperty(() => LastDateTradeInMaxPrice, value); }
        }

        public int? CarryInId
        {
            get { return GetProperty(() => CarryInId); }
            set { SetProperty(() => CarryInId, value); }
        }

        public int? CarryOutId
        {
            get { return GetProperty(() => CarryOutId); }
            set { SetProperty(() => CarryOutId, value, ChangeCarry); }
        }

        public CarryType CarryTypeOut
        {
            get { return GetProperty(() => CarryTypeOut); }
            set { SetProperty(() => CarryTypeOut, value, ChangeCarry); }
        }

        public CarryType CarryTypeIn
        {
            get { return GetProperty(() => CarryTypeIn); }
            set { SetProperty(() => CarryTypeIn, value, ChangeCarry); }
        }

        public DeliveryDataDto DeliveryDataOut
        {
            get { return GetProperty(() => DeliveryDataOut); }
            set { SetProperty(() => DeliveryDataOut, value); }
        }

        public int? CityOutId
        {
            get { return GetProperty(() => CityOutId); }
            set { SetProperty(() => CityOutId, value); }
        }

        public string TtnIn
        {
            get { return GetProperty(() => TtnIn); }
            set { SetProperty(() => TtnIn, value); }
        }

        public string TtnOut
        {
            get { return GetProperty(() => TtnOut); }
            set { SetProperty(() => TtnOut, value); }
        }

        public bool IsDocumentTypeChanged
        {
            get { return GetProperty(() => IsDocumentTypeChanged); }
            set { SetProperty(() => IsDocumentTypeChanged, value); }
        }

        public int? LegalEntityId
        {
            get { return GetProperty(() => LegalEntityId); }
            set { SetProperty(() => LegalEntityId, value); }
        }

        public bool NeedSerialNumber
        {
            get { return GetProperty(() => NeedSerialNumber); }
            set { SetProperty(() => NeedSerialNumber, value, () => RaisePropertyChanged(nameof(SerialNumber))); }
        }

        public bool HasCompletedDocuments
        {
            get { return GetProperty(() => HasCompletedDocuments); }
            set { SetProperty(() => HasCompletedDocuments, value); }
        }

        public string NpCourierCallBarcode
        {
            get { return GetProperty(() => NpCourierCallBarcode); }
            set { SetProperty(() => NpCourierCallBarcode, value); }
        }

        public string NpCourierCallInterval
        {
            get { return GetProperty(() => NpCourierCallInterval); }
            set { SetProperty(() => NpCourierCallInterval, value); }
        }

        public int? NewComplaintsCount
        {
            get { return GetProperty(() => NewComplaintsCount); }
            set { SetProperty(() => NewComplaintsCount, value, () => RaisePropertyChanged(nameof(ComplaintsCountString))); }
        }

        public int? ComplaintsCount
        {
            get { return GetProperty(() => ComplaintsCount); }
            set { SetProperty(() => ComplaintsCount, value, () => RaisePropertyChanged(nameof(ComplaintsCountString))); }
        }

        public bool CustomerPackChanged => PackId != CustomerPackId;

        public bool CustomerWarrantyChanged => WarrantyId != CustomerWarrantyId;

        public bool CustomerClassChanged => ClassId != CustomerClassId;

        public bool CustomerBrandChanged => Brand != CustomerBrand;

        public bool CustomerModelOrPnChanged => ModelOrPn != CustomerModelOrPn;

        public bool AllowEdit => StateId == TradeInState.New.Id;

        public string ComplaintsCountString => $"{NewComplaintsCount ?? 0}/{ComplaintsCount ?? 0}";

        public string BuyoutAmountString => BuyoutAmount > 0 ? $"{BuyoutAmount?.ToString("F2")} грн" : string.Empty;

        public string RealBuyoutAmountString => RealBuyoutAmount > 0 ? $"{RealBuyoutAmount?.ToString("F2")} грн" : string.Empty;

        public string MilitaryTaxAmountString => MilitaryTaxAmount > 0 ? $"{MilitaryTaxAmount?.ToString("F2")} грн" : string.Empty;

        public string MilitaryTaxValueString => MilitaryTaxValue > 0 ? $"{(MilitaryTaxValue.Value * 100).ToString("F1")}%" : string.Empty;

        public string PdfTaxAmountString => PdfTaxAmount > 0 ? $"{PdfTaxAmount?.ToString("F2")} грн" : string.Empty;

        public bool OrderFound => OrderId.HasValue && StateId == TradeInState.New.Id;

        public bool RedMarkerForEvaluatedOn => EvaluatedOn.HasValue && EvaluatedOn.Value.AddDays(3) < DateTime.Now;

        public string ProductName => this.GetLocalName(LocalizableNameType.Ukr);

        public static void BuildMetadata(MetadataBuilder<TradeInViewItem> builder)
        {
            builder.Property(x => x.Phone).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.FirstName)
                .MaxLength(20, () => "Значение не может быть длиннее 20 символов")
                .ApplyClientNameRusUkrValidationRules()
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.OrderId).MatchesInstanceRule((x, y) => !y.BoughtInTelemart || x.HasValue, () => "Введите номер заказа");
            builder.Property(x => x.ProductId).MatchesInstanceRule((x, y) => !y.BoughtInTelemart || x.HasValue, () => "Выберите из списка");
            builder.Property(x => x.CategoryId).MatchesInstanceRule((x, y) => !string.IsNullOrEmpty(y.Brand) || x != null, () => "Выберите категорию или введите бренд");
            builder.Property(x => x.Brand).MatchesInstanceRule((x, y) => y.CategoryId > 0 || !string.IsNullOrEmpty(x), () => "Введите бренд или выберите категорию");
            builder.Property(x => x.ClassId).MatchesRule(x => x > 0, () => "Укажите состояние товара");
            builder.Property(x => x.PackId).MatchesRule(x => x > 0, () => "Укажите сoстояние упаковки");
            builder.Property(x => x.WarrantyId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Email).MatchesRegularExpression(RegexConstants.EmailRegex, () => Resources.TradeInViewModel_Email);
            builder.Property(x => x.SerialNumber)
                .MatchesInstanceRule((x, y) => y.NeedSerialNumber != true || !string.IsNullOrEmpty(x), () => "Поле обязательно к заполнению")
                .MatchesRule(x => string.IsNullOrEmpty(x) || !x.Contains(' '), () => "Введите серийный номер без пробелов");
            builder.Property(x => x.ProductId).MatchesInstanceRule((x, y) => y.OrderId == null || x.HasValue, () => "Не заполнен товар");
            builder.Property(x => x.CityOutId).MatchesInstanceRule((x, y) => y.CarryOutId == null || x.HasValue, () => "Для способа доставки НП город обязательное поле");
            builder.Property(x => x.DeliveryDataOut).MatchesInstanceRule((x, y) => y.CarryOutId == null || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.TtnIn)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrEmpty(x)
                              || string.IsNullOrWhiteSpace(y.CarryTypeIn?.TtnRegex)
                              || Regex.IsMatch(x, y.CarryTypeIn.TtnRegex),
                    () => "Введите корректно ТТН поступления");
        }

        private void ChangeCarry()
        {
            if (CarryOutId == CarryType.PickupId)
            {
                CityOutId = null;
            }

            RaisePropertiesChanged(nameof(CityOutId), nameof(DeliveryDataOut), nameof(LastName), nameof(MiddleName));
        }
    }
}