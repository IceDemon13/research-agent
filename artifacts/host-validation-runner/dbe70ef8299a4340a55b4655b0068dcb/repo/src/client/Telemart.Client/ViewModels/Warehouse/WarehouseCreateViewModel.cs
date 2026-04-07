using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseCreateViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyCollection<EmployeeDto> _employees;

        public WarehouseCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public WarehouseCreateViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<WarehouseKind> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int? TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value, TypeChanged); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value, ChangeEmployee); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public int? EmployeeAssemblyId
        {
            get { return GetProperty(() => EmployeeAssemblyId); }
            set { SetProperty(() => EmployeeAssemblyId, value); }
        }

        public ComboBoxItem? EmployeeAssembly
        {
            get { return GetProperty(() => EmployeeAssembly); }
            set { SetProperty(() => EmployeeAssembly, value, () => EmployeeAssemblyId = EmployeeAssembly?.Id ?? null); }
        }

        #endregion

        public bool IsReadonlyEmployeeAssembly => TypeId != WarehouseKind.Assembly.Id;

        public override int Width => 400;

        public override int Height => 200;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<WarehouseCreateViewModel> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(245, () => "Длина поля должна быть не больше чем 245 символов");
            builder.Property(x => x.TypeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.EmployeeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.EmployeeAssemblyId)
               .MatchesInstanceRule((x, y) => y.IsReadonlyEmployeeAssembly || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Phone)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(RefreshCitiesAsync(), RefreshEmployeesAsync());

            Types = Dictionaries.GetItems<WarehouseKind>().Where(x => !x.IsVirtual).ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Создание склада";

            async Task RefreshCitiesAsync()
            {
                List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

                Cities = cities
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshEmployeesAsync()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                _employees = employees;

                Employees = employees
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                Result<WarehouseDto> result = await WebClient.ExecuteApiRequestAsync(new CreateWarehouse(Mapper.Map<WarehouseCreateDto>(this)));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Склад создан с ошибками";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Склад успешно создан");
                }

                Messenger.Send(new WarehouseMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании склада");
                ShowValidationResultView("Ошибки при создании склада", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create warehouse");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании склада");
                Logger.LogError(exception, "Error while creating warehouse");
            }
        }

        private void TypeChanged()
        {
            if (TypeId != WarehouseKind.Assembly.Id)
            {
                EmployeeAssemblyId = null;
            }

            RaisePropertiesChanged(nameof(EmployeeAssemblyId), nameof(EmployeeAssembly), nameof(IsReadonlyEmployeeAssembly));
        }

        private void ChangeEmployee()
        {
            Phone = EmployeeId.HasValue ? _employees.FirstOrDefault(x => x.Id == EmployeeId)!.Phone1 : string.Empty;
        }
    }
}