using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseRouteTimeViewItem : BindableBase, IDataErrorInfo
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, () => RaisePropertiesChanged(nameof(IsNew), nameof(IsAdded))); }
        }

        public int WarehouseRouteId
        {
            get { return GetProperty(() => WarehouseRouteId); }
            set { SetProperty(() => WarehouseRouteId, value); }
        }

        public DateTime TimeOut
        {
            get { return GetProperty(() => TimeOut); }
            set { SetProperty(() => TimeOut, value); }
        }

        public DateTime TimeIn
        {
            get { return GetProperty(() => TimeIn); }
            set { SetProperty(() => TimeIn, value); }
        }

        public DateTime TimeDeparture
        {
            get { return GetProperty(() => TimeDeparture); }
            set { SetProperty(() => TimeDeparture, value); }
        }

        public DateTime TimeArrive
        {
            get { return GetProperty(() => TimeArrive); }
            set { SetProperty(() => TimeArrive, value); }
        }

        public int Days
        {
            get { return GetProperty(() => Days); }
            set { SetProperty(() => Days, value); }
        }

        public int? CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value, CarryIdChanged); }
        }

        public DeliveryTypeDto DeliveryType
        {
            get { return GetProperty(() => DeliveryType); }
            set { SetProperty(() => DeliveryType, value); }
        }

        public string DaysOfWeekOut
        {
            get { return GetProperty(() => DaysOfWeekOut); }
            set { SetProperty(() => DaysOfWeekOut, value); }
        }

        public string DaysOfWeekIn
        {
            get { return GetProperty(() => DaysOfWeekIn); }
            set { SetProperty(() => DaysOfWeekIn, value); }
        }

        public bool DeliveryTypeEnabled
        {
            get { return GetProperty(() => DeliveryTypeEnabled); }
            set { SetProperty(() => DeliveryTypeEnabled, value); }
        }

        public List<object> Purposes
        {
            get { return GetProperty(() => Purposes); }
            set { SetProperty(() => Purposes, value); }
        }

        public bool Auto
        {
            get { return GetProperty(() => Auto); }
            set { SetProperty(() => Auto, value); }
        }

        public bool IsNew => Id == 0;

        public bool IsAdded => Id < 0;

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<WarehouseRouteTimeViewItem> builder)
        {
            builder.Property(x => x.Purposes)
                .MatchesRule(x => x?.Any() == true, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.DaysOfWeekIn)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRegularExpression("[1-7]+", () => "Значение должно содержать только цифры 1-7")
                .MaxLength(7, () => "Значение поля должно быть короче 8 символов")
                .MatchesRule(IsUniqueSymbols, () => "В значении поля не должно быть повторяющихся символов");
            builder.Property(x => x.DaysOfWeekOut)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Days).MatchesInstanceRule((x, y) => x <= 7 && x >= 0, () => "Значение должно быть от 0 до 7");
            builder.Property(x => x.DaysOfWeekOut)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRegularExpression("[1-7]+", () => "Значение должно содержать только цифры 1-7")
                .MaxLength(7, () => "Значение поля должно быть короче 8 символов")
                .MatchesRule(IsUniqueSymbols, () => "В значении поля не должно быть повторяющихся символов");

            static bool IsUniqueSymbols(string s)
            {
                return s == null || s.GroupBy(y => y).All(y => y.Count() == 1);
            }
        }

        private void CarryIdChanged()
        {
            if (CarryId.HasValue && (CarryId.Value == CarryType.NpDeliveryId || CarryId.Value == CarryType.NpWarehouseId))
            {
                DeliveryTypeEnabled = true;
            }
            else
            {
                DeliveryTypeEnabled = false;
                DeliveryType = null;
            }

            RaisePropertyChanged(nameof(DeliveryType));
        }
    }
}