using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;

namespace Telemart.Client.ViewModels.Notification
{
    public class NotificationsFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly IWebClient _webClient;

        public NotificationsFilterViewModel(IWebClient webClient)
        {
            _webClient = webClient;
        }

        public void Init(
            IEnumerable<ComboBoxItem> targets,
            IEnumerable<ComboBoxItem> employees,
            IEnumerable<ComboBoxItem> notificationTypes,
            IEnumerable<ComboBoxItem> notificationEntities)
        {
            Targets = targets.ToObservableCollection();
            Employees = employees.ToObservableCollection();
            NotificationTypes = notificationTypes.ToObservableCollection();
            NotificationEntities = notificationEntities.ToObservableCollection();

            if (_webClient.AuthenticatedEmployee.HasAnyRole(Role.Admin))
            {
                AccessToAllNotifications = true;
            }
            else
            {
                SelectedEmployeeId = _webClient.AuthenticatedEmployee.Id;
            }
        }

        public void Reset()
        {
            SelectedEmployeeId = null;
            SelectedCreatedOnTo = null;
            SelectedCreatedOnFrom = null;
            SelectedTargetIds = null;
            SelectedEntityId = null;
            SelectedDocumentId = null;

            SelectedTargetIds = Targets.Select(x => x.Id).Cast<object>().ToList();
        }

        public string Error => string.Empty;

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public bool AccessToAllNotifications
        {
            get { return GetProperty(() => AccessToAllNotifications); }
            set { SetProperty(() => AccessToAllNotifications, value); }
        }

        public int? SelectedEmployeeId
        {
            get { return GetProperty(() => SelectedEmployeeId); }
            set { SetProperty(() => SelectedEmployeeId, value); }
        }

        public int? SelectedEntityId
        {
            get { return GetProperty(() => SelectedEntityId); }
            set { SetProperty(() => SelectedEntityId, value); }
        }

        public int? SelectedNotificationTypeId
        {
            get { return GetProperty(() => SelectedNotificationTypeId); }
            set { SetProperty(() => SelectedNotificationTypeId, value); }
        }

        public int? SelectedDocumentId
        {
            get { return GetProperty(() => SelectedDocumentId); }
            set { SetProperty(() => SelectedDocumentId, value); }
        }

        public List<object> SelectedTargetIds
        {
            get { return GetProperty(() => SelectedTargetIds); }
            set { SetProperty(() => SelectedTargetIds, value); }
        }

        public DateTime? SelectedCreatedOnFrom
        {
            get { return GetProperty(() => SelectedCreatedOnFrom); }
            set { SetProperty(() => SelectedCreatedOnFrom, value); }
        }

        public DateTime? SelectedCreatedOnTo
        {
            get { return GetProperty(() => SelectedCreatedOnTo); }
            set { SetProperty(() => SelectedCreatedOnTo, value); }
        }

        public ObservableCollection<ComboBoxItem> NotificationEntities
        {
            get { return GetProperty(() => NotificationEntities); }
            private set { SetProperty(() => NotificationEntities, value); }
        }

        public ObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<ComboBoxItem> Targets
        {
            get { return GetProperty(() => Targets); }
            private set { SetProperty(() => Targets, value); }
        }

        public ObservableCollection<ComboBoxItem> NotificationTypes
        {
            get { return GetProperty(() => NotificationTypes); }
            private set { SetProperty(() => NotificationTypes, value); }
        }

        public NotificationsFilteringItem GetFilteringItem()
        {
            return new NotificationsFilteringItem()
            {
                EmployeeId = SelectedEmployeeId,
                EntityId = SelectedEntityId,
                NotificationTypeId = SelectedNotificationTypeId,
                TargetIds = SelectedTargetIds?.Cast<int>().ToArray() ?? Array.Empty<int>(),
                DocumentId = SelectedDocumentId,
                CreatedOnFrom = SelectedCreatedOnFrom,
                CreatedOnTo = SelectedCreatedOnTo
            };
        }
    }
}