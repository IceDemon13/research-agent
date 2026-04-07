using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Quotas;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Quotas;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Quotas
{
    public sealed class QuotasViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        public QuotasViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IDictionaries dictionaries,
            IMapper mapper,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _errorHandler = errorHandler;

            RefreshQuotasCommand = new AsyncCommand(RefreshAsync);
            AddQuotasCommand = new DelegateCommand(AddQuotas);
            EditQuotaCommand = new AsyncCommand<QuotaViewItem>(EditQuotaAsync, x => x != null);
            DeleteQuotaCommand = new AsyncCommand<QuotaViewItem>(DeleteQuotaAsync, x => x != null);
            messenger.Register<QuotasCreateMessage>(this, OnQuotasCreate);
        }

        public QuotasViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshQuotasCommand { get; }

        public IDelegateCommand AddQuotasCommand { get; }

        public IAsyncCommand EditQuotaCommand { get; }

        public IAsyncCommand DeleteQuotaCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<QuotaViewItem> Quotas
        {
            get { return GetProperty(() => Quotas); }
            set { SetProperty(() => Quotas, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Departments
        {
            get { return GetProperty(() => Departments); }
            set { SetProperty(() => Departments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public QuotaViewItem SelectedQuotaViewItem
        {
            get { return GetProperty(() => SelectedQuotaViewItem); }
            set { SetProperty(() => SelectedQuotaViewItem, value); }
        }

        #endregion

        #region DialogSettings

        public override int MinWidth => 400;

        public override int Width => 700;

        public override int MaxWidth => 1920;

        public override int MinHeight => 400;

        public override int Height => 600;

        public override int MaxHeight => 1080;

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            await RefreshAsync();

            Title = "Квоты";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            await Task.WhenAll(LoadedQuotasAsync(), LoadDepartmentsAsync(), LoadEmployeesAsync());
        }

        private async Task LoadedQuotasAsync()
        {
            List<QuotaDto> quotas = await WebClient.ExecuteApiRequestAsync(new QueryQuotas());

            Quotas = quotas.OrderByDescending(x => x.QuotaDate)
                .Select(x => _mapper.Map<QuotaViewItem>(x)).ToObservableCollection();
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            Departments = dtos.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue).ToReadOnlyObservableCollection();
        }

        private void AddQuotas()
        {
            DialogDocumentManagerService.ShowView<QuotasAddViewModel>(null, this);
        }

        private async Task DeleteQuotaAsync(QuotaViewItem item)
        {
            Result<object> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DeleteQuota(item.Id)),
                "удалении квоты",
                "Квота удалена",
                this,
                true,
                showDialog: true);

            if (result?.IsSuccess == true)
            {
                Quotas.Remove(item);
            }
        }

        private async Task EditQuotaAsync(QuotaViewItem item)
        {
            ComboBoxItem? department = Departments?.FirstOrDefault(x => x.Id == item.DepartmentId);

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter(
                    "Введите значение квоты",
                    $"Отдел {department?.DisplayValue} на {item.QuotaDate:MM.yyyy}",
                    "^[0-9]{1,3}$",
                    "Допустимое значение 0….999."),
                this);

            if (viewModel?.IsOk == true && int.TryParse(viewModel.Content, out int quotaValue))
            {
                QuotaSaveDto quotaSaveDto = new QuotaSaveDto()
                {
                    QuotaValue = quotaValue
                };

                Result<QuotaDto> quotaResult = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdateQuota(item.Id, quotaSaveDto)),
                    "обновлении квоты",
                    "Квота измененена",
                    this,
                    true,
                    showDialog: true);

                if (quotaResult?.IsSuccess == true)
                {
                    Quotas.DoActionWithItem(x => x.Id == quotaResult.Data.Id, viewItem => _mapper.Map(quotaResult.Data, viewItem));
                }
            }
        }

        private void OnQuotasCreate(QuotasCreateMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Quotas.AddRange(message.Quotas.Select(x => _mapper.Map<QuotaViewItem>(x)).ToObservableCollection());
                    break;
            }
        }
    }
}