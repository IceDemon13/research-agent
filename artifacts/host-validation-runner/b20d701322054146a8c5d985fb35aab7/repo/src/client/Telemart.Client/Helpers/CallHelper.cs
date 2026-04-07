using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.ViewModels.Dialogs;

namespace Telemart.Client.Helpers
{
    public sealed class CallHelper : ICallHelper
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IWebClient _webClient;

        public CallHelper(IErrorHandler errorHandler, IWebClient webClient)
        {
            _errorHandler = errorHandler;
            _webClient = webClient;
        }

        public async Task CreateCallByOrderAsync(OrderDto order, int callTypeId, Priority priority, string titleGetContent, string contentDefault, ISupportServices parent)
        {
            if (!string.IsNullOrEmpty(titleGetContent))
            {
                IDocumentManagerService dialogDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

                GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                    "Текст",
                    titleGetContent);

                GetTextFromUserViewModel fromUserViewModel = dialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, parent);

                if (!fromUserViewModel.IsOk)
                {
                    return;
                }

                contentDefault = fromUserViewModel.Content;
            }

            CallCreateDto callCreateDto = new CallCreateDto
            {
                OrderId = order.Id,
                Task = contentDefault,
                CallTypeId = callTypeId,
                SubdivisionId = Subdivision.Telemart.Id,
                PriorityId = priority.Id,
                Phone = order.Phone,
                Phone2 = order.Phone2,
                CallFrom = null,
                CallTo = null,
                Incoming = false,
                ResponsibleEmployeeId = order.ConfirmedBy ?? order.ManagerEmployeeId,
                ContractorId = order.ClientId,
                Fio = order.Fio
            };

            await _errorHandler.HandleErrorsAsync(
                _ => _webClient.ExecuteApiRequestAsync(new CreateCall(callCreateDto)),
                "создании звонка",
                "Звонок создан",
                parent,
                true,
                showNotification: true);
        }
    }
}