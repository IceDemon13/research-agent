using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderSetManagerViewModel : TelemartDialogViewModelBase
    {
        private int orderId;

        public OrderSetManagerViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public int CurrentManagerId
        {
            get { return GetProperty(() => CurrentManagerId); }
            private set { SetProperty(() => CurrentManagerId, value); }
        }

        public int? NewManagerId
        {
            get { return GetProperty(() => NewManagerId); }
            set { SetProperty(() => NewManagerId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public static void BuildMetadata(MetadataBuilder<OrderSetManagerViewModel> builder)
        {
            builder.Property(x => x.NewManagerId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            Title = "Выберите менеджера";
            await base.HandleLoadedAsync();
        }

        protected override void OnParameterChanged(object param)
        {
            OrderSetManagerParameter parameter = (OrderSetManagerParameter)param;

            orderId = parameter.OrderId;
            CurrentManagerId = parameter.ManagerId;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new OrderSetManager(orderId, NewManagerId.Value));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Менеджер изменен с предупреждениями";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Менеджер успешно изменен");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении менеджера");
                ShowValidationResultView("Ошибки при изменении менеджера", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to set manager to order");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении менеджера");
                Logger.LogError(exception, "Error while setting manager to order");
            }
        }
    }
}