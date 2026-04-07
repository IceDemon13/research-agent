using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.AssemblyService.Actions;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Call;
using static Telemart.Client.Data.Requests.Features.AssemblyService.Actions.DefectAssemblyServiceProduct;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public sealed class AssemblyServiceDefectViewModel : ProductDefectViewModelBase<AssemblyServiceDefectResultDto, DefectAssemblyDto>
    {
        private AssemblyServiceDto _assemblyService;
        private volatile bool _createCall;

        public AssemblyServiceDefectViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, "Дефект сборки")
        {
            Messenger = messenger;
        }

        private IMessenger Messenger { get; }

        protected override CallEntityActionWithBodyRequestResultBase<AssemblyServiceDefectResultDto, DefectAssemblyDto> GetDefectRequest()
        {
            return new DefectAssemblyServiceProduct(_assemblyService.Id, SelectedProduct!.Value.Id, SerialNumber, StatedDefect, _createCall);
        }

        protected override IEnumerable<ProductComboBoxItem> GetProducts()
        {
            return _assemblyService.Products
                .Where(x => x.ScannedQuantity > 0).Select(x => new ProductComboBoxItem(x.Id, x.Name, x.KeepSerial));
        }

        protected override async Task<bool> BeforeProcessOkAsync()
        {
            List<int> callStates = new List<int>();
            callStates.Add(CallState.NewId);

            PagedResult<CallDto> calls = await WebClient.ExecuteApiRequestAsync(new QueryCalls(new CallFilteringItem()
            {
                OrderIds = _assemblyService.OrderId.ToString(),
                CallStates = callStates
            }));

            _createCall = calls?.Data?.Any() != true || MessageFacadeService.Confirm("Уже создан один звонок в статусе Новый, вы хотите создать еще один?");

            return true;
        }

        protected override async Task AfterSuccessOkAsync(AssemblyServiceDefectResultDto result)
        {
            Messenger.Send(new AssemblyServiceMessage(result.AssemblyService, MessageType.Changed));
        }

        protected override async Task HandleLoadedAsync()
        {
            AssemblyServiceDefectParameter parameter = (AssemblyServiceDefectParameter)Parameter;
            _assemblyService = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyService(parameter.Id));

            await base.HandleLoadedAsync();
        }
    }
}