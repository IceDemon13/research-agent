using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Notification
{
    public class NotificationViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int NotificationTypeId
        {
            get { return GetProperty(() => NotificationTypeId); }
            set { SetProperty(() => NotificationTypeId, value); }
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
        }

        public int EntityId
        {
            get { return GetProperty(() => EntityId); }
            set { SetProperty(() => EntityId, value); }
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public int TargetId
        {
            get { return GetProperty(() => TargetId); }
            set { SetProperty(() => TargetId, value); }
        }

        public bool Received
        {
            get { return GetProperty(() => Received); }
            set { SetProperty(() => Received, value); }
        }

        public bool Read
        {
            get { return GetProperty(() => Read); }
            set { SetProperty(() => Read, value); }
        }

        public DateTime? ReadOn
        {
            get { return GetProperty(() => ReadOn); }
            set { SetProperty(() => ReadOn, value); }
        }

        public DateTime? ReceivedOn
        {
            get { return GetProperty(() => ReceivedOn); }
            set { SetProperty(() => ReceivedOn, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }
    }
}