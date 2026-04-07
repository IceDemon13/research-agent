using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Organization
{
    public class OrganizationContactViewModel : TelemartDialogViewModelBase
    {
        public OrganizationContactViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        #region INPC

        public OrganizationContactViewItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        public OrganizationContactViewItem ResultItem
        {
            get { return GetProperty(() => ResultItem); }
            set { SetProperty(() => ResultItem, value); }
        }

        public ObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<OrganizationPosition> Positions
        {
            get { return GetProperty(() => Positions); }
            set { SetProperty(() => Positions, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            Model = (OrganizationContactViewItem)Parameter;

            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            Employees = employees.Where(x => x.Active).OrderBy(x => x.Name).ToObservableCollection();

            Positions = Dictionaries.GetItems<OrganizationPosition>().ToObservableCollection();

            Title = Model.Id > 0
                ? "Изменение контакта"
                : "Создание контакта";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                OrganizationContactDto saveDto = Mapper.Map<OrganizationContactDto>(Model);

                Task<Result<OrganizationContactDto>> saveTask = Model.Id > 0
                    ? WebClient.ExecuteApiRequestAsync(new UpdateOrganizationContact(Model.OrganizationId, saveDto))
                    : WebClient.ExecuteApiRequestAsync(new CreateOrganizationContact(Model.OrganizationId, saveDto));

                Result<OrganizationContactDto> result = await saveTask;

                ResultItem = Mapper.Map(result.Data, Model);
                MessageFacadeService.ShowNotificationInfo("Контакт успешно сохранен");
                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении контакта");
                ShowValidationResultView("Ошибки при сохранении контакта", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save organization account");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении контакта");
                ShowValidationResultView("Ошибки при сохранении контакта", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save organization account");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении контакта");
            }
        }
    }
}