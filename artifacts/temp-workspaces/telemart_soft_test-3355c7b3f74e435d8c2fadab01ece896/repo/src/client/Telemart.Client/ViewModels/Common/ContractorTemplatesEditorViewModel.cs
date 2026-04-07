using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Data.Extensions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.ErrorHandling;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor.Template;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Contractor;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class ContractorTemplatesEditorViewModel : TelemartDialogViewModelBase
    {
        public ContractorTemplatesEditorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            HandleSelectedContractorChangedCommand = new AsyncCommand<ContractorViewItem>(HandleSelectedContractorChangedAsync);
            CreateContractorTemplateCommand = new AsyncCommand(CreateContractorTemplateAsync, CanCreateContractorTemplate);
            SetDefaultCommand = new AsyncCommand<bool>(SetDefaultAsync, x => CanSetDefault());
            SetNonDefaultCommand = new AsyncCommand<bool>(SetDefaultAsync, x => CanSetNonDefault());
            DeleteContractorTemplateCommand = new AsyncCommand(DeleteContractorTemplateAsync, CanDeleteContractorTemplate);
        }

        public ContractorTemplatesEditorViewModel()
        {
        }

        public event Action Cancel;

        public event Action ApplyTemplate;

        #region Commands

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IAsyncCommand HandleSelectedContractorChangedCommand { get; }

        public IAsyncCommand CreateContractorTemplateCommand { get; }

        public IAsyncCommand DeleteContractorTemplateCommand { get; }

        public IAsyncCommand SetDefaultCommand { get; }

        public IAsyncCommand SetNonDefaultCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ContractorTemplateDto> ContractorTemplates
        {
            get { return GetProperty(() => ContractorTemplates); }
            set { SetProperty(() => ContractorTemplates, value); }
        }

        public ContractorViewItem SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value, () => HandleSelectedContractorChangedCommand.Execute(value)); }
        }

        public ContractorTemplateDto SelectedContractorTemplate
        {
            get { return GetProperty(() => SelectedContractorTemplate); }
            set { SetProperty(() => SelectedContractorTemplate, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<ContractorTemplatesEditorViewModel> builder)
        {
            builder.Property(x => x.SelectedContractor).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedContractorTemplate).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleOkAsync()
        {
            ApplyTemplate?.Invoke();
            return Task.CompletedTask;
        }

        protected override void HandleCancel()
        {
            Cancel?.Invoke();
        }

        private bool CanCreateContractorTemplate()
        {
            return SelectedContractor != null;
        }

        private bool CanDeleteContractorTemplate()
        {
            return SelectedContractorTemplate != null && SelectedContractorTemplate.Id > 0 && SelectedContractor != null;
        }

        private bool CanSetDefault()
        {
            return SelectedContractorTemplate != null && ContractorTemplates.All(y => !y.IsDefault);
        }

        private bool CanSetNonDefault()
        {
            return SelectedContractorTemplate != null;
        }

        private async Task CreateContractorTemplateAsync()
        {
            try
            {
                CreateContractorTemplateViewModel viewModel = DialogDocumentManagerService.ShowView<CreateContractorTemplateViewModel>(SelectedContractor.Id, this);

                if (viewModel.IsOk)
                {
                    ContractorTemplateDto templateFromServer = await WebClient.ExecuteApiRequestAsync(new CreateContractorTemplate(viewModel.ContractorTemplateToSave));
                    ContractorTemplates.Add(templateFromServer);
                    MessageFacadeService.ShowNotificationInfo("Шаблон успешно создан");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save contractor template");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении шаблона");
            }
        }

        private async Task DeleteContractorTemplateAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteContractorTemplate(SelectedContractorTemplate.Id));
                ContractorTemplates.Remove(SelectedContractorTemplate);
                MessageFacadeService.ShowNotificationInfo("Шаблон успешно удалён");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete contractor template");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении шаблона");
            }
        }

        private async Task SetDefaultAsync(bool isDefault)
        {
            try
            {
                ContractorTemplateDto templateFromServer = await WebClient.ExecuteApiRequestAsync(new SetDefaultContractorTemplate(SelectedContractorTemplate.Id, isDefault));

                int index = ContractorTemplates.FindIndex(x => x.Id == templateFromServer.Id);
                ContractorTemplates.RemoveAt(index);
                ContractorTemplates.Insert(index, templateFromServer);

                SelectedContractorTemplate = templateFromServer;

                string text = isDefault
                    ? $"Шаблон '{templateFromServer.Name}' теперь шаблон по-умолчанию для {SelectedContractor.Name}"
                    : $"Шаблон '{templateFromServer.Name}' теперь не является шаблоном по-умолчанию для {SelectedContractor.Name}";

                MessageFacadeService.ShowNotificationInfo(text);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set template as default");
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении шаблона по-умолчанию");
            }
        }

        private async Task HandleSelectedContractorChangedAsync(ContractorViewItem contractor)
        {
            try
            {
                ContractorTemplates = null;
                SelectedContractorTemplate = null;
                List<ContractorTemplateDto> templates = await WebClient.ExecuteApiRequestAsync(new QueryContractorTemplates(contractor.Id)).GetPagedResultDataAsync().ConfigureAwait(false);
                ContractorTemplates = new ObservableCollection<ContractorTemplateDto>(templates);

                if (ContractorTemplates.Any())
                {
                    SelectedContractorTemplate = ContractorTemplates.FirstOrDefault(x => x.IsDefault) ?? ContractorTemplates.First();
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get contractor templates");
                MessageFacadeService.ShowNotificationError("Ошибка при получении шаблонов");
            }
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                CancelCommand.Execute(null);
            }
        }
    }
}