using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Barcode;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.WarehouseCell;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Warehouse.Cell;

namespace Telemart.Client.ViewModels.Common.RecognizeWarehouseCell
{
    public class RecognizeWarehouseCellBarcodeViewModel : RecognizeBarcodeViewModelBase<RecognizeWarehouseCellBarcodeResultEventArgs>
    {
        private RecognizeWarehouseCellBarcodeParameter parameter;

        public RecognizeWarehouseCellBarcodeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            UseQuantity = false;
        }

        protected override void OnParameterChanged(object parameter)
        {
            this.parameter = (RecognizeWarehouseCellBarcodeParameter)parameter;
        }

        protected override void FireOnFinished(RecognizeWarehouseCellBarcodeResultEventArgs args)
        {
            base.FireOnFinished(args);

            if (args.IsValid)
            {
                BarcodeRecognitionViewItem recognitionItem = new BarcodeRecognitionViewItem(RecognizeBarcodeMessageType.Info, "Ячейка распознана", DateTime.Now);
                RecognitionViewItems.Insert(0, recognitionItem);
                SelectedRecognitionItem = recognitionItem;
            }
        }

        protected override async Task RecognizeBarcodeAsync(string barcodeText)
        {
            if (string.IsNullOrEmpty(barcodeText))
            {
                return;
            }

            try
            {
                if (string.Equals(barcodeText, BarcodeConstants.CmdFinish, StringComparison.Ordinal))
                {
                    FireOnFinishCommand();
                    return;
                }

                FireOnStarted();

                OurWarehouseCellBarcode barcode = new OurWarehouseCellBarcode(barcodeText);

                WarehouseCellFilteringItem filteringItem = new WarehouseCellFilteringItem(parameter.WarehouseId);

                filteringItem.Used = parameter.Used;
                filteringItem.Active = true;

                if (barcode.IsValid)
                {
                    filteringItem.Ids = new[] { barcode.CellId };
                }
                else
                {
                    filteringItem.Name = barcodeText;
                }

                QueryWarehouseCells query = new QueryWarehouseCells(filteringItem);

                List<WarehouseCellDto> warehouseCells = await WebClient.ExecuteApiRequestAsync(query);
                WarehouseCellDto warehouseCell = warehouseCells.FirstOrDefault();

                RecognizeWarehouseCellBarcodeResultEventArgs args = warehouseCell == null
                    ? RecognizeWarehouseCellBarcodeResultEventArgs.Error(barcodeText, "Ячейка не найдена либо занята")
                    : RecognizeWarehouseCellBarcodeResultEventArgs.Found(warehouseCell, barcodeText);

                FireOnFinished(args);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to recognize product by barcode");
                FireOnFinished(RecognizeWarehouseCellBarcodeResultEventArgs.Error(barcodeText, "Ошибка при распознавании ШК"));
            }
            finally
            {
                BarcodeText = string.Empty;
            }
        }
    }
}