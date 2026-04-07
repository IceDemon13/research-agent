using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Common
{
    public static class NavCommands
    {
        public static IDelegateCommand NavigateShowcaseHistoryCommand { get; } = new DelegateCommand(
            () =>
            {
                Messenger.Default.Send(new ShowcaseHistoriesMessage());
            });
    }
}