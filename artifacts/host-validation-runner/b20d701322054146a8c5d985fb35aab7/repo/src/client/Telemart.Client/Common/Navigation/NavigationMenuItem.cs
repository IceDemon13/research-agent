using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Common.Navigation
{
    public sealed class NavigationMenuItem : BindableBase
    {
        private readonly IReadOnlyCollection<string> allowedByRoles;
        private readonly IReadOnlyCollection<BusinessOperation> allowedByOperations;
        private readonly IReadOnlyCollection<BusinessOperation> disallowedByOperation;
        private readonly NavigationMode navigationMode;
        private readonly object parameter;
        private readonly Type viewType;

        public NavigationMenuItem(
            string header,
            string imageName,
            Type viewType,
            IReadOnlyCollection<Role> allowedByRoles,
            IReadOnlyCollection<BusinessOperation> allowedByOperations,
            int countOfSimultaneouslyOpenTabs = 1,
            string headerToolTip = null,
            NavigationMode navigationMode = NavigationMode.Tab,
            object viewModel = null,
            object parameter = null,
            IReadOnlyCollection<BusinessOperation> disallowedByOperation = null)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (countOfSimultaneouslyOpenTabs < 1)
            {
                throw new ArgumentException($"{nameof(countOfSimultaneouslyOpenTabs)} must be greater then 1", nameof(countOfSimultaneouslyOpenTabs));
            }

            Header = header;

            if (!string.IsNullOrWhiteSpace(imageName))
            {
                Image = $"pack://application:,,,/Telemart.Client;component/Images/NavigationMenu/{imageName}.png";
                HasImage = true;
            }
            else
            {
                HasImage = false;
            }

            this.viewType = viewType;
            this.allowedByOperations = allowedByOperations;
            this.allowedByRoles = allowedByRoles?.Select(x => x.Name).ToArray();
            CountOfSimultaneouslyOpenTabs = countOfSimultaneouslyOpenTabs;
            HeaderToolTip = headerToolTip;
            this.navigationMode = navigationMode;
            this.parameter = parameter;
            this.disallowedByOperation = disallowedByOperation;

            ViewModel = viewModel;
        }

        public int CountOfSimultaneouslyOpenTabs
        {
            get { return GetProperty(() => CountOfSimultaneouslyOpenTabs); }
            private set { SetProperty(() => CountOfSimultaneouslyOpenTabs, value, () => { RaisePropertyChanged(nameof(CanBeOpenedSimultaneously)); }); }
        }

        public string Header
        {
            get { return GetProperty(() => Header); }
            private set { SetProperty(() => Header, value); }
        }

        public string HeaderToolTip
        {
            get { return GetProperty(() => HeaderToolTip); }
            private set { SetProperty(() => HeaderToolTip, value); }
        }

        public string Image
        {
            get { return GetProperty(() => Image); }
            private set { SetProperty(() => Image, value); }
        }

        public bool HasImage
        {
            get { return GetProperty(() => HasImage); }
            private set { SetProperty(() => HasImage, value); }
        }

        public object ViewModel { get; }

        public bool CanBeOpenedSimultaneously => CountOfSimultaneouslyOpenTabs > 1;

        public bool Allowed(IReadOnlyCollection<string> employeeRoles, IReadOnlyCollection<BusinessOperation> employeeOperations)
        {
            if (employeeRoles?.Contains(Role.Admin.Name) == true)
            {
                return true;
            }

            return ((allowedByRoles == null || (employeeRoles != null && allowedByRoles.Any(employeeRoles.Contains)))
                   || (allowedByOperations == null || (employeeOperations != null && allowedByOperations.Any(employeeOperations.Contains))))
                && (disallowedByOperation == null || disallowedByOperation.Any(employeeOperations.Contains) == false);
        }

        public NavigationMode GetNavigationMode()
        {
            return navigationMode;
        }

        public Type GetViewType()
        {
            return viewType;
        }

        public object GetParameter()
        {
            return parameter;
        }

        public void SetHeader(string header)
        {
            Header = header;
        }
    }
}