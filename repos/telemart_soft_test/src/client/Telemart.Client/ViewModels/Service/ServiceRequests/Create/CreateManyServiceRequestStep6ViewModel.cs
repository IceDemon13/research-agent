using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateManyServiceRequestStep6ViewModel :
        WizardPageViewModelBase<CreateManyServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardFinishCommand
    {
        public CreateManyServiceRequestStep6ViewModel(IMessenger messenger, IMessageFacadeService messageFacadeService)
        {
            MessageFacadeService = messageFacadeService;
            Messenger = messenger;
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            EditServiceRequestCommand = new DelegateCommand<ServiceRequestViewItem>(EditServiceRequest, x => x != null);
        }

        public CreateManyServiceRequestStep6ViewModel()
        {
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand EditServiceRequestCommand { get; }

        public bool CanFinish => true;

        public bool CanGoBack => Model.ValidationItems?.Any(x => x.IsError) == true;

        public override string Description => GetDescription();

        public override string Header { get; } = "Результат";

        private IWizardService WizardService => GetService<IWizardService>();

        private IMessenger Messenger { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        public void OnFinish(CancelEventArgs e)
        {
        }

        public void OnGoBack(CancelEventArgs e)
        {
            Model.ValidationItems = null;
            Model.CreatedServiceRequests = null;

            WizardService.NavigateToView<CreateManyServiceRequestStep5ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        private Task HandleLoadedAsync()
        {
            if (Model.CreatedServiceRequestsDto != null)
            {
                if (Model.CreatedServiceRequestsDto.Count > 1)
                {
                    MessageFacadeService.ShowNotificationInfo($"Создана группа СЗ №{Model.CreatedServiceRequestsDto.Select(x => x.Group.GroupId).FirstOrDefault()}");
                }

                foreach (ServiceRequestDto serviceRequest in Model.CreatedServiceRequestsDto)
                {
                    Messenger.Send(new ServiceRequestMessage(serviceRequest, MessageType.Added));
                }
            }

            return Task.CompletedTask;
        }

        private string GetDescription()
        {
            if (Model.ValidationItems != null && Model.ValidationItems.Any())
            {
                return "Ошибки при создании заявок";
            }

            if (Model.CreatedServiceRequestsDto.Count > 1)
            {
                return $"Группа сервисных заявок №{Model.CreatedServiceRequestsDto.Select(x => x.Group.GroupId).FirstOrDefault()} успешно создана";
            }

            return "Сервисная заявка успешно создана";
        }

        private void EditServiceRequest(ServiceRequestViewItem viewItem)
        {
            Messenger.Send(new ServiceRequestViewMessage(viewItem.Id));
        }
    }
}