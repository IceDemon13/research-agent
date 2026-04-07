using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class SelectProductPageViewModel :
        WizardPageViewModelBase<CreateServiceRequestModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        private readonly IWebClient webClient;

        public SelectProductPageViewModel(IWebClient webClient)
        {
            this.webClient = webClient;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            HandleProductChangedCommand = new DelegateCommand<EditValueChangedEventArgs>(HandleProductChanged);
        }

        public SelectProductPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand HandleProductChangedCommand { get; }

        #endregion

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public bool CanGoBack { get; } = true;

        public bool CanGoForward => Model.SelectProductDataValid();

        public override string Description { get; } = "Необходимо заполнить информацию о товаре";

        public override string Header { get; } = "Выбор товара";

        public ReadOnlyObservableCollection<ComboBoxItem> Groups
        {
            get { return GetProperty(() => Groups); }
            private set { SetProperty(() => Groups, value); }
        }

        private IWizardService WizardService => GetService<IWizardService>();

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateServiceRequestModel.ProductId),
            nameof(CreateServiceRequestModel.SerialNumber)
        };

        public void OnGoBack(CancelEventArgs e)
        {
            WizardService.NavigateToView<SelectOrderPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            e.Cancel = true;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            if (!IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
            {
                WizardService.NavigateToView<DeclarantPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            }

            e.Cancel = true;
        }

        private async Task HandleLoadedAsync()
        {
            List<ContractorDto> contractorsList = await webClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
            Contractors = contractorsList.ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshGroupsAsync());
        }

        private async Task RefreshGroupsAsync()
        {
            List<ServiceRequestGroupDto> groups = await webClient.ExecuteApiRequestAsync(new QueryServiceRequestsGroup(GetServiceRequestsGroupFilteringItem()), true);

            Groups = groups
                .Select(x => new ComboBoxItem(x.GroupId, x.GroupId.ToString(), true))
                .ToReadOnlyObservableCollection();
        }

        private static ServiceRequestsGroupFilteringItem GetServiceRequestsGroupFilteringItem()
        {
            ServiceRequestsGroupFilteringItem item = new ServiceRequestsGroupFilteringItem
            {
                Finished = false
            };

            return item;
        }

        private void HandleProductChanged(EditValueChangedEventArgs arg)
        {
            int? productId = (int?)arg.NewValue;

            if (productId.HasValue)
            {
                Model.SetSerials(productId.Value);

                if (Model.SerialNumbers.Count == 1)
                {
                    Model.SerialNumber = Model.SerialNumbers.First();
                }
            }
        }
    }
}
