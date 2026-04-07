using System.ComponentModel;
using System.Globalization;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ProgressScreenViewModel : ViewModelBase, IDocumentContent
    {
        public ProgressScreenViewModel(string title, int totalCount)
        {
            ProcessedCount = 0;
            TotalCount = totalCount;

            OkCommand = new DelegateCommand(HandleOk);
            CancelCommand = new DelegateCommand(HandleCancel);

            Title = title;
        }

        public IDelegateCommand OkCommand { get; }

        public IDelegateCommand CancelCommand { get; }

        public int TotalCount
        {
            get { return GetProperty(() => TotalCount); }
            set { SetProperty(() => TotalCount, value, () => RaisePropertiesChanged(nameof(ProgressContent), nameof(ProgressValue))); }
        }

        public int ProcessedCount
        {
            get { return GetProperty(() => ProcessedCount); }
            set { SetProperty(() => ProcessedCount, value, () => RaisePropertiesChanged(nameof(ProgressContent), nameof(ProgressValue))); }
        }

        public string ProgressContent => $"{ProcessedCount.ToString(CultureInfo.InvariantCulture)}/{TotalCount.ToString(CultureInfo.InvariantCulture)}";

        public double ProgressValue => (ProcessedCount / (double)TotalCount) * 100;

        public bool IsOk { get; set; }

        public IDocumentOwner DocumentOwner { get; set; }

        public object Title { get; }

        private IDispatcherService DispatcherService => GetService<IDispatcherService>();

        public void SetProcessedCount(int processed)
        {
            ProcessedCount = processed;

            if (ProcessedCount == TotalCount)
            {
                DispatcherService.BeginInvoke(() => { OkCommand.Execute(null); });
            }
        }

        public void OnClose(CancelEventArgs e)
        {
        }

        public void OnDestroy()
        {
        }

        private void HandleCancel()
        {
            Close();
        }

        private void HandleOk()
        {
            IsOk = true;
            Close();
        }

        private void Close()
        {
            DocumentOwner.Close(this, false);
        }
    }
}
