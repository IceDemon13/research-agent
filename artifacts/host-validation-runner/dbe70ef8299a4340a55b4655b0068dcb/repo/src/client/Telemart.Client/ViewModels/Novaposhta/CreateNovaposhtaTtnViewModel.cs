using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Contact;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.NovaposhtaTtn;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Novaposhta
{
    public sealed class CreateNovaposhtaTtnViewModel : TelemartDialogViewModelBase
    {
        public CreateNovaposhtaTtnViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMediator mediator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Mediator = mediator;

            SelectNpWarehouseCommand = new DelegateCommand(SelectNpWarehouse, () => CityId.HasValue && ServiceTypeId.HasValue);
        }

        public CreateNovaposhtaTtnViewModel()
        {
        }

        public IDelegateCommand SelectNpWarehouseCommand { get; }

        #region INPC

        public ReadOnlyObservableCollection<NpContractorDto> NpContractors
        {
            get { return GetProperty(() => NpContractors); }
            private set { SetProperty(() => NpContractors, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ContractorContactDto> ContractorContacts
        {
            get { return GetProperty(() => ContractorContacts); }
            private set { SetProperty(() => ContractorContacts, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<NovaposhtaTtnSource> TtnSources
        {
            get { return GetProperty(() => TtnSources); }
            private set { SetProperty(() => TtnSources, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PayerTypes
        {
            get { return GetProperty(() => PayerTypes); }
            private set { SetProperty(() => PayerTypes, value); }
        }

        public ReadOnlyObservableCollection<NovaposhtaTtnServiceType> ServiceTypes
        {
            get { return GetProperty(() => ServiceTypes); }
            private set { SetProperty(() => ServiceTypes, value); }
        }

        #region Sender

        public string NpContractorRef
        {
            get { return GetProperty(() => NpContractorRef); }
            set { SetProperty(() => NpContractorRef, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public EmployeeDto Sender
        {
            get { return GetProperty(() => Sender); }
            set { SetProperty(() => Sender, value, () => SenderPhone = Sender?.Phone1); }
        }

        public string SenderPhone
        {
            get { return GetProperty(() => SenderPhone); }
            set { SetProperty(() => SenderPhone, value); }
        }

        #endregion

        #region Recipient

        public ContractorDto SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value, ContractorChanged); }
        }

        public string Edrpou
        {
            get { return GetProperty(() => Edrpou); }
            set { SetProperty(() => Edrpou, value, () => RaisePropertyChanged(nameof(VisibilityEdrpou))); }
        }

        public ContractorContactDto SelectedContractorContact
        {
            get { return GetProperty(() => SelectedContractorContact); }
            set { SetProperty(() => SelectedContractorContact, value, ContractorContactChanged); }
        }

        public string RecipientLastName
        {
            get { return GetProperty(() => RecipientLastName); }
            set { SetProperty(() => RecipientLastName, value); }
        }

        public string RecipientFirstName
        {
            get { return GetProperty(() => RecipientFirstName); }
            set { SetProperty(() => RecipientFirstName, value); }
        }

        public string RecipientMiddleName
        {
            get { return GetProperty(() => RecipientMiddleName); }
            set { SetProperty(() => RecipientMiddleName, value); }
        }

        public string RecipientPhone
        {
            get { return GetProperty(() => RecipientPhone); }
            set { SetProperty(() => RecipientPhone, value); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value, () => DeliveryData = null); }
        }

        public int? ServiceTypeId
        {
            get { return GetProperty(() => ServiceTypeId); }
            set { SetProperty(() => ServiceTypeId, value, () => DeliveryData = null); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value, () => RecipientAddress = DeliveryData == null ? null : RecipientAddress); }
        }

        public string RecipientAddress
        {
            get { return GetProperty(() => RecipientAddress); }
            private set { SetProperty(() => RecipientAddress, value); }
        }

        public bool RecipientReadOnly => SelectedContractor?.IsClient == false && SelectedContractorContact != null;

        #endregion

        public int? TtnSourceId
        {
            get { return GetProperty(() => TtnSourceId); }
            set { SetProperty(() => TtnSourceId, value); }
        }

        public int? PayerTypeId
        {
            get { return GetProperty(() => PayerTypeId); }
            set { SetProperty(() => PayerTypeId, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public decimal PackageInsurance
        {
            get { return GetProperty(() => PackageInsurance); }
            set { SetProperty(() => PackageInsurance, value); }
        }

        public decimal PackagePlaces
        {
            get { return GetProperty(() => PackagePlaces); }
            set { SetProperty(() => PackagePlaces, value); }
        }

        public decimal PackageWeight
        {
            get { return GetProperty(() => PackageWeight); }
            set { SetProperty(() => PackageWeight, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool NotAddToApplication
        {
            get { return GetProperty(() => NotAddToApplication); }
            set { SetProperty(() => NotAddToApplication, value); }
        }

        public bool VisibilityEdrpou => !string.IsNullOrEmpty(Edrpou);

        #endregion

        private IMapper Mapper { get; }

        private IMediator Mediator { get; }

        public static void BuildMetadata(MetadataBuilder<CreateNovaposhtaTtnViewModel> builder)
        {
            builder.Property(x => x.NpContractorRef)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.WarehouseId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Sender)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRule(x => !string.IsNullOrEmpty(x?.Name) && x.Name.Length <= 100, () => "ФИО введено некорректно")
                .MatchesRule(x => x != null && Regex.IsMatch(x.Name, @"[\p{IsCyrillic}\s\.-`']*"), () => "Допускаются только символы русского/украинского алфавита");

            builder.Property(x => x.SenderPhone)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedContractor)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RecipientLastName)
                .Required(() => Resources.RequiredErrorMessage)
                .ApplyFioPartValidationRules();

            builder.Property(x => x.RecipientFirstName)
                .Required(() => Resources.RequiredErrorMessage)
                .ApplyFioPartValidationRules();

            builder.Property(x => x.RecipientMiddleName)
                .ApplyFioPartValidationRules();

            builder.Property(x => x.RecipientPhone)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.ServiceTypeId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RecipientAddress)
                .MatchesInstanceRule((x, y) => y.DeliveryData != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.TtnSourceId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Description)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PayerTypeId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PackagePlaces).NpPackagePlaces();
            builder.Property(x => x.PackageWeight).NpPackageWeight();
            builder.Property(x => x.PackageInsurance).NpPackageInsurance(200m);

            builder.Property(x => x.Comment)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(
                QueryNpContractorsAsync(),
                QueryWarehousesAsync(),
                QueryEmployeesAsync(),
                QueryContractorsAsync(),
                QueryCitiesAsync());

            TtnSources = Dictionaries.GetItems<NovaposhtaTtnSource>().ToReadOnlyObservableCollection();
            PayerTypes = GetPayerTypes().ToReadOnlyObservableCollection();
            ServiceTypes = Dictionaries.GetItems<NovaposhtaTtnServiceType>().ToReadOnlyObservableCollection();

            Sender = Employees.FirstOrDefault(x => x.Id == WebClient.AuthenticatedEmployee.Id);

            Title = "Создание ТТН";

            IEnumerable<ComboBoxItem> GetPayerTypes()
            {
                yield return new ComboBoxItem(NovaposhtaTtnPayer.Sender.Id, $"{NovaposhtaTtnPayer.Sender.Name} (Telemart.ua)");
                yield return new ComboBoxItem(NovaposhtaTtnPayer.Recipient.Id, NovaposhtaTtnPayer.Recipient.Name);
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if ((string.IsNullOrEmpty(Edrpou) && !MessageFacadeService.Confirm("TTH будет создана на физ. лицо, потому что не заполнено поле ЄДРПОУ (ОКПО). Продолжить?"))
                || (!string.IsNullOrEmpty(Edrpou) && !MessageFacadeService.Confirm("TTH будет создана на юр. лицо. Продолжить?")))
            {
                return;
            }

            try
            {
                NovaposhtaTtnCreateDto dto = new NovaposhtaTtnCreateDto
                {
                    NpContractorRef = NpContractorRef,
                    SenderFio = Sender.Name,
                    SenderPhone = Sender.Phone1,
                    SenderWarehouseId = WarehouseId.Value,
                    RecipientContractorId = SelectedContractor.Id,
                    RecipientLastName = RecipientLastName,
                    RecipientFistName = RecipientFirstName,
                    RecipientMiddleName = RecipientMiddleName,
                    RecipientPhone = RecipientPhone,
                    RecipientDeliveryData = DeliveryData,
                    SourceId = TtnSourceId.Value,
                    PayerTypeId = PayerTypeId.Value,
                    ServiceType = ServiceTypeId.Value,
                    Description = Description,
                    PackageInsurance = PackageInsurance,
                    PackagePlaces = (int)PackagePlaces,
                    PackageWeight = (double)PackageWeight,
                    Comment = Comment,
                    Edrpou = Edrpou,
                    AddToApplication = !NotAddToApplication,

                };

                Result<NpDocumentDto> result = await WebClient.ExecuteApiRequestAsync(new CreateNpTtn(dto));

                Clipboard.SetText(result.Data.Id);

                try
                {
                    await Mediator.Send(new PrintTrackNumberRequest(result.Data.Id, result.Data.Link, true));
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Error while downloading TTN. Print TTN manually.");
                    MessageFacadeService.ShowNotificationError("Ошибка при скачивании ТТН. Распечатайте ТТН вручную.");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании ТТН");
                ShowValidationResultView("Ошибки при создании ТТН", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create track number");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании ТТН");
                Logger.LogError(exception, "Failed to create service request track number");
            }
        }

        private async Task QueryNpContractorsAsync()
        {
            List<NpContractorDto> npContractors = await WebClient.ExecuteApiRequestAsync(new QueryNpContractors(), true);

            NpContractors = npContractors
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task QueryWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Service.Id || x.TypeId == WarehouseKind.ShowCase.Id || x.TypeId == WarehouseKind.Pickup.Id))
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task QueryEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task QueryContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Contractors = contractors
                .Where(x => !x.IsCompetitor && !x.IsFolder && x.Active && WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.SubdivisionId))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task QueryCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            Cities = cities
                .Where(x => x.Active)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void ContractorChanged()
        {
            if (SelectedContractor == null)
            {
                ContractorContacts = null;
                Edrpou = null;
            }
            else
            {
                List<ContractorContactDto> contractorContacts = WebClient.ExecuteApiRequest(new QueryContractorContacts(SelectedContractor.Id));

                ContractorContacts = contractorContacts
                    .Where(x => x.Active)
                    .OrderBy(x => x.FullName)
                    .ToReadOnlyObservableCollection();

                Edrpou = SelectedContractor.Edrpou;
            }

            RaisePropertiesChanged(nameof(RecipientReadOnly), nameof(Edrpou));
        }

        private void ContractorContactChanged()
        {
            if (SelectedContractorContact != null)
            {
                Fio fio = new Fio(SelectedContractorContact.FullName);

                RecipientLastName = fio.LastName;
                RecipientFirstName = fio.FirstName;
                RecipientMiddleName = fio.MiddleName;
                RecipientPhone = SelectedContractorContact.Phone1;
                CityId = SelectedContractorContact.CityId;
            }
            else
            {
                RecipientLastName = null;
                RecipientFirstName = null;
                RecipientMiddleName = null;
                RecipientPhone = null;
                CityId = null;
            }

            RaisePropertyChanged(nameof(RecipientReadOnly));
        }

        private void SelectNpWarehouse()
        {
            if (CityId == null || ServiceTypeId == null)
            {
                return;
            }

            CarryType carryType = Dictionaries.GetItemById<CarryType>(GetCarryType());

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                carryType.Id,
                CityId.Value,
                RecipientAddress,
                DeliveryData);

            if (carryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    RecipientAddress = viewModel.FullAddressString;
                }
            }
            else if (carryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    RecipientAddress = viewModel.Place.PlaceName;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private int GetCarryType()
        {
            switch (ServiceTypeId)
            {
                case NovaposhtaTtnServiceType.DoorsDoorsId:
                case NovaposhtaTtnServiceType.WarehouseDoorsId:
                    return CarryType.NpDeliveryId;
                case NovaposhtaTtnServiceType.DoorsWarehouseId:
                case NovaposhtaTtnServiceType.WarehouseWarehouseId:
                    return CarryType.NpWarehouseId;
            }

            throw new NotSupportedException();
        }
    }
}