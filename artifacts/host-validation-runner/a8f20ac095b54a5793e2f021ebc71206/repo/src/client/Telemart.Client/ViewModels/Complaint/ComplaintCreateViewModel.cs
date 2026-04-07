using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Complaint;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Complaint
{
    public sealed class ComplaintCreateViewModel : TelemartDialogViewModelBase
    {
        private ComplaintCreateParameter parameter;

        public ComplaintCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public ComplaintCreateViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<ComplaintSourceDto> Sources
        {
            get { return GetProperty(() => Sources); }
            private set { SetProperty(() => Sources, value); }
        }

        public ReadOnlyObservableCollection<ComplaintTypeDto> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public int? SourceId
        {
            get { return GetProperty(() => SourceId); }
            set { SetProperty(() => SourceId, value); }
        }

        public ComplaintTypeDto Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public int? PriorityId
        {
            get { return GetProperty(() => PriorityId); }
            set { SetProperty(() => PriorityId, value); }
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int? BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public DateTime? DeadLine
        {
            get { return GetProperty(() => DeadLine); }
            set { SetProperty(() => DeadLine, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public bool ProductsVisible
        {
            get { return GetProperty(() => ProductsVisible); }
            set { SetProperty(() => ProductsVisible, value); }
        }

        public bool OpenAfterCreate
        {
            get { return GetProperty(() => OpenAfterCreate); }
            set { SetProperty(() => OpenAfterCreate, value); }
        }

        #endregion

        public DateTime MinDeadLineValue { get; } = DateTime.Today;

        public DateTime MaxDeadLineValue { get; } = DateTime.Today.AddDays(30);

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<ComplaintCreateViewModel> builder)
        {
            builder.Property(x => x.SourceId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Type)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PriorityId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Text)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(1000, () => "Значение поля должно быть короче 1000 символов");
            builder.Property(x => x.DeadLine)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Fio)
                .ApplyFioValidationRules(() => "Введите ФИО");
            builder.Property(x => x.Phone)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.EmployeeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Email)
                .MaxLength(250, () => "Значение поля должно быть короче 250 символов")
                .MatchesRegularExpression(@"^$|^.+@.+\..+$", () => Resources.OrderViewModel_Email);
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (ComplaintCreateParameter)Parameter;

            Priorities = Dictionaries.GetItems<Priority>()
                .OrderByDescending(p => p.Id)
                .ToReadOnlyObservableCollection();

            Products = parameter.Products?.ToReadOnlyObservableCollection();
            ProductsVisible = Products?.Any() == true;
            Fio = parameter.Fio;
            Phone = parameter.Phone;
            Phone2 = parameter.Phone2;
            Email = parameter.Email;

            OpenAfterCreate = false;

            PriorityId = Priority.Normal.Id;

            await Task.WhenAll(QueryEmployees(), QueryTypes(), QuerySources());

            await base.HandleLoadedAsync();

            Title = "Создание жалобы";

            async Task QueryEmployees()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                Employees = employees
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task QueryTypes()
            {
                List<ComplaintTypeDto> types = await WebClient.ExecuteApiRequestAsync(new QueryComplaintTypes(), true);

                Types = types
                    .Where(x => x.Active)
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }

            async Task QuerySources()
            {
                List<ComplaintSourceDto> sourcesDtos = await WebClient.ExecuteApiRequestAsync(new QueryComplaintSources());

                Sources = sourcesDtos
                    .OrderBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (Type == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите тип жалобы");
                return;
            }

            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (Type.ParentId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите тип, а не группу");
                return;
            }

            try
            {
                ComplaintCreateDto dto = new ComplaintCreateDto
                {
                    ContractorId = parameter.ContractorId,
                    OrderId = parameter.OrderId,
                    ServiceRequestId = parameter.ServiceRequestId,
                    TradeInId = parameter.TradeInId
                };

                Result<ComplaintDto> result = await WebClient.ExecuteApiRequestAsync(new CreateComplaint(MapToDto(this, dto)));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Жалоба №{result.Data.Id} создана с предупреждениями");
                    ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Жалоба №{result.Data.Id} успешно создана");
                }

                Messenger.Send(new ComplaintMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();

                if (OpenAfterCreate)
                {
                    Messenger.Send(new ComplaintViewMessage(result.Data.Id));
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании жалобы");
                ShowValidationResultView("Ошибки при создании жалобы", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create complaint");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании жалобы");
                Logger.LogError(exception, "Error while creating complaint");
            }
        }

        private ComplaintCreateDto MapToDto(ComplaintCreateViewModel source, ComplaintCreateDto target)
        {
            target.Deadline = source.DeadLine.Value;
            target.EmployeeId = source.EmployeeId.Value;
            target.PriorityId = source.PriorityId.Value;
            target.SourceId = source.SourceId.Value;
            target.TypeId = source.Type.Id;
            target.ProductId = source.ProductId;
            target.Text = source.Text;
            target.Fio = source.Fio;
            target.Phone = source.Phone;
            target.Phone2 = source.Phone2;
            target.Email = source.Email;

            return target;
        }
    }
}