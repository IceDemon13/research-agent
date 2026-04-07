using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Business.Delivery.ScanSheets;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.CreateScanSheetForEntities
{
    public sealed class CreateScanSheetForEntitiesModel : BindableBase, IDataErrorInfo
    {
        public CreateScanSheetForEntitiesModel(int entityTypeId)
        {

            EntityTypeId = entityTypeId;
        }

        public int EntityTypeId { get; init; }

        public ScanSheetProcessorType? ScanSheetProcessorType => Business.Delivery.ScanSheets.ScanSheetProcessorType.Novaposhta;

        public DateTime? DateFrom
        {
            get { return GetProperty(() => DateFrom); }
            set { SetProperty(() => DateFrom, value); }
        }

        public DateTime? DateTo
        {
            get { return GetProperty(() => DateTo); }
            set { SetProperty(() => DateTo, value); }
        }

        public ComboBoxItem? SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public ObservableCollection<ScanSheetEntityViewItem> Entities
        {
            get { return GetProperty(() => Entities); }
            set { SetProperty(() => Entities, value); }
        }

        public ScanSheetEntityViewItem SelectedEntity
        {
            get { return GetProperty(() => SelectedEntity); }
            set { SetProperty(() => SelectedEntity, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        public ScanSheetCreateResponse[] Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value); }
        }

        public int ProgressValue
        {
            get { return GetProperty(() => ProgressValue); }
            set { SetProperty(() => ProgressValue, value); }
        }

        public string ProgressText
        {
            get { return GetProperty(() => ProgressText); }
            set { SetProperty(() => ProgressText, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<CreateScanSheetForEntitiesModel> builder)
        {
            builder.Property(x => x.ScanSheetProcessorType)
                .MatchesRule(x => x.HasValue, () => "Выберите тип реестра");

            builder.Property(x => x.DateFrom)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.DateTo)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedWarehouse)
                .MatchesRule(x => x != null, () => "Выберите склад");
        }
    }
}