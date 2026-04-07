using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Humanizer;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class CreateServiceRequestTrackNumberSourceViewItem : BindableBase, IDataErrorInfo
    {
        public string Source
        {
            get { return GetProperty(() => Source); }
            set { SetProperty(() => Source, value); }
        }

        public string Recipient
        {
            get { return GetProperty(() => Recipient); }
            set { SetProperty(() => Recipient, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public int CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value, () => RaisePropertiesChanged(nameof(Address))); }
        }

        public string Address => DeliveryData?.Address;

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<CreateServiceRequestTrackNumberSourceViewItem> builder)
        {
            builder.Property(x => x.Source).MatchesRule(
                x => string.Equals(x, "Из заявки", StringComparison.OrdinalIgnoreCase),
                x => $"запрещено выбирать данные {x}".Transform(To.SentenceCase));

            builder.Property(x => x.Recipient)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Phone)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Address).MatchesInstanceRule(
                (_, y) => y.DeliveryData != null,
                () => "Не удалось определить город или отделение НП");
        }
    }
}