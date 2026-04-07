using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using MediatR;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class NpScanSheetViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<int, string> employeesDictionary;
        private IReadOnlyDictionary<int, string> warehousesDictionary;
        private NpScanSheetDto npScanSheet;

        public NpScanSheetViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMediator mediator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mediator = mediator;
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        private IMediator Mediator { get; }

        protected override async Task HandleLoadedAsync()
        {
            NpScanSheetParameter parameter = (NpScanSheetParameter)Parameter;

            npScanSheet = await WebClient.ExecuteApiRequestAsync(new QueryNpScanSheet(parameter.ScansheetId));

            await Task.WhenAll(RefreshEmployeesAsync(), RefreshWarehousesAsync());

            SummaryItems = GetSummaryItems();

            await base.HandleLoadedAsync();

            Title = $"Реестр НП №{parameter.ScansheetId}";
        }

        protected override async Task HandleOkAsync()
        {
            await Mediator.Send(new PrintScanSheetRequest(npScanSheet.Ref, npScanSheet.NpScanSheetLink, true));

            IsOk = true;
            Close();
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            employeesDictionary = employees.ToDictionary(x => x.Id, y => y.Name);
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            warehousesDictionary = warehouses.ToDictionary(x => x.Id, y => y.Name);
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Создал", employeesDictionary.GetValueOrDefault(npScanSheet.CreatedBy));
            yield return new SummaryViewItem("Создан", npScanSheet.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat));
            yield return new SummaryViewItem("Склад", warehousesDictionary.GetValueOrDefault(npScanSheet.WarehouseId));
            yield return new SummaryViewItem("Контрагент", npScanSheet.NpContractorName);
            yield return new SummaryViewItem("Заказов", $"{npScanSheet.OrderCount}");
        }
    }
}
