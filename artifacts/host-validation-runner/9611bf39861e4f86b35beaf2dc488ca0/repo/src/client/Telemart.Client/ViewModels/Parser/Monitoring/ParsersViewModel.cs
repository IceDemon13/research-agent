using System;
using DevExpress.Mvvm;
using Telemart.Client.Data.Options;

namespace Telemart.Client.ViewModels.Parser.Monitoring
{
    public sealed class ParsersViewModel : ViewModelBase
    {
        public ParsersViewModel(SchedulerOptions options)
        {
            Url = new Uri(options.BaseAddress);
        }

        public Uri Url
        {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value); }
        }
    }
}