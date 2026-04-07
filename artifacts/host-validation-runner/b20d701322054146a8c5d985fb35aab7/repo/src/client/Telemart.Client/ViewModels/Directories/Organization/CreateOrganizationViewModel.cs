using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
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
    public sealed class CreateOrganizationViewModel : TelemartDialogViewModelBase
    {
        public CreateOrganizationViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Ownerships = Dictionaries.GetItems<OrganizationOwnership>().ToReadOnlyObservableCollection();
            IsVatPayer = true;
        }

        #region INPC

        public OrganizationOwnership SelectedOwnership
        {
            get { return GetProperty(() => SelectedOwnership); }
            set { SetProperty(() => SelectedOwnership, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public bool IsVatPayer
        {
            get { return GetProperty(() => IsVatPayer); }
            set { SetProperty(() => IsVatPayer, value); }
        }

        public ReadOnlyObservableCollection<OrganizationOwnership> Ownerships
        {
            get { return GetProperty(() => Ownerships); }
            private set { SetProperty(() => Ownerships, value); }
        }

        #endregion

        #region Commands

        #endregion

        public OrganizationDto ResultOrganization { get; set; }

        public static void BuildMetadata(MetadataBuilder<CreateOrganizationViewModel> builder)
        {
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedOwnership).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            Title = "Создание организации";
            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            OrganizationCreateDto createDto = new OrganizationCreateDto
            {
                OwnershipId = SelectedOwnership.Id,
                Name = Name,
                IsVatPayer = IsVatPayer
            };

            try
            {
                Result<OrganizationDto> result = await WebClient.ExecuteApiRequestAsync(new CreateOrganization(createDto));

                ResultOrganization = result.Data;
                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании организации");
                ShowValidationResultView("Ошибки при создании организации", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create organization");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании организации");
                Logger.LogError(exception, "Error while creating organization");
            }
        }
    }
}