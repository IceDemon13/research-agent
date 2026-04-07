using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AssemblyTest;
using Telemart.Client.Data.Requests.Features.AssemblyTest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public sealed class AssemblyTestGroupMoveViewModel : TelemartDialogViewModelBase
    {
        private AssemblyTestGroupMoveParameter parameter;

        public AssemblyTestGroupMoveViewModel(IWebClient webClient, IMessageFacadeService messageFacadeService, IDictionaries dictionaries, IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public ReadOnlyObservableCollection<HierarchicalItem> TestGroups
        {
            get { return GetProperty(() => TestGroups); }
            set { SetProperty(() => TestGroups, value); }
        }

        public HierarchicalItem? SelectedGroup
        {
            get { return GetProperty(() => SelectedGroup); }
            set { SetProperty(() => SelectedGroup, value); }
        }

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            List<AssemblyTestGroupDto> groups = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyTestGroups());

            TestGroups = groups
                .Select(x => new HierarchicalItem(x.Id, x.Name, x.ParentId ?? 0))
                .Union(new[] { new HierarchicalItem(0, "[Корень]") })
                .ToReadOnlyObservableCollection();

            Title = "Перенос группы тестов";

            await base.HandleLoadedAsync();
        }

        protected override void OnParameterChanged(object parameter)
        {
            this.parameter = (AssemblyTestGroupMoveParameter)parameter;

            base.OnParameterChanged(parameter);
        }

        protected override async Task HandleOkAsync()
        {
            if (SelectedGroup == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите папку");
                return;
            }

            try
            {
                Result<AssemblyTestGroupDto> result = await WebClient.ExecuteApiRequestAsync(new MoveAssemblyTestGroup(parameter.GroupId, SelectedGroup.Value.Id == 0 ? null : (int?)SelectedGroup.Value.Id));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    const string message = "Папка перемещена с предупреждениями";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Папка успешно перемещена");
                }

                Messenger.Send(new EntityMessage<AssemblyTestGroupDto>(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при перемещении папки");
                ShowValidationResultView("Ошибки при перемещении группы", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to move assembly test group");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при перемещении папки");
                Logger.LogError(exception, "Error while moving assembly test group");
            }
        }
    }
}