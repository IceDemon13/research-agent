using System;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.MvvmEnhancements
{
    public sealed class WizardDialogViewModel<TModel> : ViewModelBase
    {
        private readonly TModel model;
        private readonly Type startPageViewModelType;

        public WizardDialogViewModel(Type startPageViewModelType, TModel model, object parentViewModel)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (parentViewModel == null)
            {
                throw new ArgumentNullException(nameof(parentViewModel));
            }

            this.model = model;
            this.startPageViewModelType = startPageViewModelType;

            ((ISupportParentViewModel)this).ParentViewModel = parentViewModel;

            HandleWizardLoadedCommand = new DelegateCommand(HandleWizardLoaded);
        }

        public IDelegateCommand HandleWizardLoadedCommand { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private void HandleWizardLoaded()
        {
            WizardService.NavigateToView(startPageViewModelType, model, this);
        }
    }
}