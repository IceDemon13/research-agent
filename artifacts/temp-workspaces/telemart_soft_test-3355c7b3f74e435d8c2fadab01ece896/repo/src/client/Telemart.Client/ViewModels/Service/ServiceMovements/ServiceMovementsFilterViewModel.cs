using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Service.ServiceMovements
{
    public class ServiceMovementsFilterViewModel : BindableBase, IDataErrorInfo
    {
        public ServiceMovementsFilterViewModel(IDictionaries dictionaries)
        {
            States = new[] { MovementState.Left, MovementState.Arrived, MovementState.Received, MovementState.Cancelled }.ToReadOnlyObservableCollection();

            Carries = dictionaries.GetItems<CarryType>().Where(x => x.Active).ToReadOnlyObservableCollection();
        }

        public ReadOnlyObservableCollection<MovementState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<CarryType> Carries
        {
            get { return GetProperty(() => Carries); }
            set { SetProperty(() => Carries, value); }
        }

        public string MovementNumbers
        {
            get { return GetProperty(() => MovementNumbers); }
            set { SetProperty(() => MovementNumbers, value); }
        }

        public int? WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public int? WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public List<object> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        public ObservableCollection<CarryType> SelectedCarries
        {
            get { return GetProperty(() => SelectedCarries); }
            set { SetProperty(() => SelectedCarries, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ServiceMovementsFilterViewModel> builder)
        {
            builder.Property(x => x.MovementNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public ServiceMovementsFilteringItem GetFilteringItem()
        {
            ServiceMovementsFilteringItem item = new ServiceMovementsFilteringItem
            {
                ServiceMovementsNumbers = MovementNumbers,
                WarehouseFromId = WarehouseFromId,
                WarehouseToId = WarehouseToId,
                Statuses = SelectedStatuses?.Cast<int>().ToList(),
                Carries = SelectedCarries.Select(x => x.Id).ToList(),
                Ttn = TrackNumber
            };

            return item;
        }

        public void ResetFilterValues()
        {
            MovementNumbers = null;
            WarehouseFromId = null;
            WarehouseToId = null;
            SelectedStatuses = new List<object> { MovementState.Left.Id, MovementState.Arrived.Id };
            SelectedCarries = new ObservableCollection<CarryType>();
            TrackNumber = string.Empty;
        }
    }
}
