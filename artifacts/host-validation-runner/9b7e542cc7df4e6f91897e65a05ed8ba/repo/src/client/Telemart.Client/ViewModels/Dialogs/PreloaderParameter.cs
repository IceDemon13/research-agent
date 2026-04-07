using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Dialogs
{
    public class PreloaderParameter
    {
        public PreloaderParameter(Func<IProgress<string>, Task<IEnumerable<ValidationResultItem>>> action, string validationDialogTitle = "Ошибки", string isSpecialErrorMessage = "", bool canClose = true)
        {
            Action = action;
            ValidationDialogTitle = validationDialogTitle;
            IsSpecialErrorMessage = isSpecialErrorMessage;
            CanClose = canClose;
        }

        public string ValidationDialogTitle { get; }

        public Func<IProgress<string>, Task<IEnumerable<ValidationResultItem>>> Action { get; }

        public string IsSpecialErrorMessage { get; }

        public bool CanClose { get; }
    }
}