using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public class PackListCreateViewItem : BindableBase, IDataErrorInfo
    {
        public DateTime? Time
        {
            get { return GetProperty(() => Time); }
            set { SetProperty(() => Time, value); }
        }

        public DateTime Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value); }
        }

        public ObservableCollection<int> CarryIds
        {
            get { return GetProperty(() => CarryIds); }
            set { SetProperty(() => CarryIds, value); }
        }

        public OrderStatus State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public ComboBoxItem Warehouse
        {
            get { return GetProperty(() => Warehouse); }
            set { SetProperty(() => Warehouse, value); }
        }

        public int? SubdivisionId
        {
            get { return GetProperty(() => SubdivisionId); }
            set { SetProperty(() => SubdivisionId, value); }
        }

        public int? PackagerEmployeeId
        {
            get { return GetProperty(() => PackagerEmployeeId); }
            set { SetProperty(() => PackagerEmployeeId, value); }
        }

        public int? CollectorEmployeeId
        {
            get { return GetProperty(() => CollectorEmployeeId); }
            set { SetProperty(() => CollectorEmployeeId, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<PackListCreateViewItem> builder)
        {
            builder.Property(x => x.Time)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Date)
                .MatchesRule(x => x >= DateTime.Today, () => "Дата создания листа на сборку не может быть меньше текущей");

            builder.Property(x => x.CarryIds)
                .MatchesInstanceRule((x, y) => x != null && x.Any(), () => "Выберите способ доставки");

            builder.Property(x => x.SubdivisionId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PackagerEmployeeId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CollectorEmployeeId)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}
