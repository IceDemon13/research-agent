using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Editors;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class GetTextFromUserViewModel : TelemartDialogViewModelBase
    {
        public GetTextFromUserViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            MaskType = MaskType.None;
        }

        public GetTextFromUserViewModel()
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

        public string ContentMask
        {
            get { return GetProperty(() => ContentMask); }
            set { SetProperty(() => ContentMask, value); }
        }

        public bool UseMask
        {
            get { return GetProperty(() => UseMask); }
            set { SetProperty(() => UseMask, value); }
        }

        public MaskType MaskType
        {
            get { return GetProperty(() => MaskType); }
            set { SetProperty(() => MaskType, value); }
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

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value, () => { RaisePropertyChanged(nameof(Content)); }); }
        }

        public bool IsMultiline
        {
            get { return GetProperty(() => IsMultiline); }
            set { SetProperty(() => IsMultiline, value); }
        }

        public KeyGesture SelectKey
        {
            get { return GetProperty(() => SelectKey); }
            set { SetProperty(() => SelectKey, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<GetTextFromUserViewModel> builder)
        {
            builder.Property(x => x.Content)
                .MatchesInstanceRule((x, y) => !y.Required || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Content).MatchesInstanceRule(
                (x, y) => ((y.Content != null && (string.IsNullOrWhiteSpace(y.RegexPattern) || Regex.IsMatch(y.Content, y.RegexPattern))) || string.IsNullOrWhiteSpace(y.Content)),
                (x, y) => y.ErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            GetTextFromUserParameter p = (GetTextFromUserParameter)Parameter;

            ErrorMessage = p.ErrorMessage;
            RegexPattern = p.RegexPattern;
            ContentCaption = p.ContentCaption;
            Title = p.Title;
            Content = p.DefaultContent;
            IsMultiline = p.IsMultiline;
            Required = p.Required;

            if (!string.IsNullOrEmpty(p.ContentMask))
            {
                ContentMask = p.ContentMask;
                UseMask = true;
                MaskType = MaskType.Simple;
            }

            SelectKey = IsMultiline
                ? new KeyGesture(Key.Enter, ModifierKeys.Control)
                : new KeyGesture(Key.Enter);

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