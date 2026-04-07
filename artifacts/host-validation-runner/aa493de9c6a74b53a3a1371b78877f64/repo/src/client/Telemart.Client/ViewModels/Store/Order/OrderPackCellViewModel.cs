using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.WarehouseCell;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Warehouse.Cell;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.RecognizeWarehouseCell;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderPackCellViewModel : TelemartDialogViewModelBase
    {
        private readonly HashSet<int> selectedCellIds = new HashSet<int>();
        private OrderPackCellParameter parameter;

        public OrderPackCellViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            RecognizeBarcodeViewModel = new RecognizeWarehouseCellBarcodeViewModel(webClient, dictionaries, messageFacadeService);

            RecognizeBarcodeViewModel.OnStarted += (s, e) => IsRecognitionInProgress = true;
            RecognizeBarcodeViewModel.OnFinishCommand += (s, e) => IsRecognitionInProgress = false;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;

            DeleteCellCommand = new DelegateCommand<WarehouseCellDto>(RemoveWarehouseCell, x => x != null);
        }

        public IDelegateCommand DeleteCellCommand { get; }

        public RecognizeBarcodeViewModelBase<RecognizeWarehouseCellBarcodeResultEventArgs> RecognizeBarcodeViewModel { get; }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public ObservableCollection<WarehouseCellDto> WarehouseCells
        {
            get { return GetProperty(() => WarehouseCells); }
            private set { SetProperty(() => WarehouseCells, value); }
        }

        public override int Width => 700;

        public override int Height => 500;

        public override int MinWidth => 700;

        public override int MinHeight => 500;

        protected override async Task HandleLoadedAsync()
        {
            if (parameter.CellIds?.Any() == true)
            {
                WarehouseCellFilteringItem warehouseCellFiltering = new WarehouseCellFilteringItem(parameter.WarehouseId);

                warehouseCellFiltering.Ids = parameter.CellIds;
                warehouseCellFiltering.Active = true;

                List<WarehouseCellDto> warehouseCells = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseCells(warehouseCellFiltering));

                SetWarehouseCells(warehouseCells);
            }
            else
            {
                SetWarehouseCells(Enumerable.Empty<WarehouseCellDto>());
            }

            Title = "Хранение заказа";
        }

        protected override void OnParameterChanged(object parameter)
        {
            base.OnParameterChanged(parameter);

            this.parameter = (OrderPackCellParameter)parameter;

            if (parameter != null)
            {
                ((ISupportParameter)RecognizeBarcodeViewModel).Parameter = new RecognizeWarehouseCellBarcodeParameter(this.parameter.WarehouseId, false);
            }
        }

        protected override Task HandleOkAsync()
        {
            SyncWarehouseCellsWithSelectedIds();

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeWarehouseCellBarcodeResultEventArgs e)
        {
            if (e.IsValid)
            {
                if (!TryAddWarehouseCell(e.Cell))
                {
                    e.ErrorText = "Ячейка уже просканирована";
                }
            }

            IsRecognitionInProgress = false;
        }

        private void RemoveWarehouseCell(WarehouseCellDto warehouseCell)
        {
            if (warehouseCell == null)
            {
                return;
            }

            selectedCellIds.Remove(warehouseCell.Id);
            WarehouseCells?.Remove(warehouseCell);
        }

        private void SetWarehouseCells(IEnumerable<WarehouseCellDto> warehouseCells)
        {
            WarehouseCells = warehouseCells?.ToObservableCollection() ?? new ObservableCollection<WarehouseCellDto>();

            selectedCellIds.Clear();

            foreach (WarehouseCellDto warehouseCell in WarehouseCells)
            {
                selectedCellIds.Add(warehouseCell.Id);
            }
        }

        private void SyncWarehouseCellsWithSelectedIds()
        {
            if (WarehouseCells == null)
            {
                WarehouseCells = new ObservableCollection<WarehouseCellDto>();
                return;
            }

            foreach (WarehouseCellDto warehouseCell in WarehouseCells.Where(x => !selectedCellIds.Contains(x.Id)).ToArray())
            {
                WarehouseCells.Remove(warehouseCell);
            }
        }

        private bool TryAddWarehouseCell(WarehouseCellDto warehouseCell)
        {
            if (warehouseCell == null || !selectedCellIds.Add(warehouseCell.Id))
            {
                return false;
            }

            WarehouseCells.Add(warehouseCell);
            return true;
        }
    }
}
