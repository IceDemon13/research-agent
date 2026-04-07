using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public class GroupMoveViewModel : TelemartDialogViewModelBase
    {
        private GroupMoveParameter groupMoveParameter;

        public GroupMoveViewModel(IWebClient webClient, IMessageFacadeService messageFacadeService, IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public int? ToGroupId { get; private set; }

        public HierarchicalItem? SelectedGroup
        {
            get { return GetProperty(() => SelectedGroup); }
            set { SetProperty(() => SelectedGroup, value); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> Groups
        {
            get { return GetProperty(() => Groups); }
            set { SetProperty(() => Groups, value); }
        }

        protected override void OnParameterChanged(object parameter)
        {
            groupMoveParameter = (GroupMoveParameter)parameter;
            Title = groupMoveParameter.Title;
            Groups = groupMoveParameter.Groups;

            base.OnParameterChanged(parameter);
        }

        protected override Task HandleOkAsync()
        {
            if (SelectedGroup == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите папку");
                return Task.CompletedTask;
            }

            ToGroupId = SelectedGroup.Value.Id == 0 ? null : (int?)SelectedGroup.Value.Id;

            IsOk = true;
            Close();
            return Task.CompletedTask;
        }
    }
}