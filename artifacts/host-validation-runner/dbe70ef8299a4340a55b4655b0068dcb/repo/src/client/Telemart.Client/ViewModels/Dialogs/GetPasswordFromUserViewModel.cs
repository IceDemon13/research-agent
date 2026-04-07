using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class GetPasswordFromUserViewModel : TelemartDialogViewModelBase
    {
        public GetPasswordFromUserViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public GetPasswordFromUserViewModel()
        {
        }

        #region INPC

        public string Content
        {
            get { return GetProperty(() => Content); }
            set { SetProperty(() => Content, value); }
        }

        public string ContentCaption
        {
            get { return GetProperty(() => ContentCaption); }
            set { SetProperty(() => ContentCaption, value); }
        }

        public string ErrorMessage
        {
            get { return GetProperty(() => ErrorMessage); }
            set { SetProperty(() => ErrorMessage, value, () => { RaisePropertyChanged(nameof(Content)); }); }
        }

        public string RegexPattern
        {
            get { return GetProperty(() => RegexPattern); }
            set { SetProperty(() => RegexPattern, value, () => { RaisePropertyChanged(nameof(Content)); }); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<GetPasswordFromUserViewModel> builder)
        {
            builder.Property(x => x.Content).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Content).MatchesInstanceRule(
                (x, y) => string.IsNullOrWhiteSpace(y.RegexPattern) || (y.Content != null && Regex.IsMatch(y.Content, y.RegexPattern)),
                (x, y) => y.ErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            GetPasswordFromUserParameter p = (GetPasswordFromUserParameter)Parameter;

            ErrorMessage = p.ErrorMessage;
            RegexPattern = p.RegexPattern;
            ContentCaption = p.ContentCaption;
            Title = p.Title;
            Content = p.DefaultContent;

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}
