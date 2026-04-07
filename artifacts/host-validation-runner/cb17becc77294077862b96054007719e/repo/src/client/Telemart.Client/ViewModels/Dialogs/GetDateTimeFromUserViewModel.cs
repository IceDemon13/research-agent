using System;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class GetDateTimeFromUserViewModel : TelemartDialogViewModelBase
    {
        public GetDateTimeFromUserViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public GetDateTimeFromUserViewModel()
        {
        }

        #region INPC

        public DateTime? DateTime
        {
            get { return GetProperty(() => DateTime); }
            set { SetProperty(() => DateTime, value); }
        }

        public string ContentCaption
        {
            get { return GetProperty(() => ContentCaption); }
            private set { SetProperty(() => ContentCaption, value); }
        }

        public DateTime? DateTimeMin
        {
            get { return GetProperty(() => DateTimeMin); }
            private set { SetProperty(() => DateTimeMin, value); }
        }

        public DateTime? DateTimeMax
        {
            get { return GetProperty(() => DateTimeMax); }
            private set { SetProperty(() => DateTimeMax, value); }
        }

        public bool TimeSupport
        {
            get { return GetProperty(() => TimeSupport); }
            private set { SetProperty(() => TimeSupport, value); }
        }

        public bool NowIsMinTime { get; private set; }

        #endregion

        public static void BuildMetadata(MetadataBuilder<GetDateTimeFromUserViewModel> builder)
        {
            builder.Property(x => x.DateTime)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => y.DateTimeMin == null || x >= y.DateTimeMin, (x, y) => $"Значение не может быть меньше чем {y.DateTimeMin:dd.MM.yy}")
                .MatchesInstanceRule((x, y) => y.DateTimeMax == null || x <= y.DateTimeMax, (x, y) => $"Значение не может быть больше чем {y.DateTimeMax:dd.MM.yy}")
                .MatchesInstanceRule((x, y) => !y.NowIsMinTime || x >= System.DateTime.Now, (x, y) => $"Значение не может быть меньше чем текущее время");
        }

        protected override Task HandleLoadedAsync()
        {
            GetDateTimeFromUserParameter p = (GetDateTimeFromUserParameter)Parameter;

            ContentCaption = p.ContentCaption;
            Title = p.Title;

            DateTimeMin = p.MinDateTime;
            DateTimeMax = p.MaxDateTime;
            DateTime = p.DefaultDateTime;

            TimeSupport = p.WithTime;
            NowIsMinTime = p.NowIsMinTime;

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }
    }
}