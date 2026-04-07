using System;
using Newtonsoft.Json.Linq;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Tasks
{
    public sealed class TaskViewItem : TelemartEditorViewItemBase
    {
        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public TaskState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public string Task
        {
            get { return GetProperty(() => Task); }
            set { SetProperty(() => Task, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public DateTime Deadline
        {
            get { return GetProperty(() => Deadline); }
            set { SetProperty(() => Deadline, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
        }

        public JObject Documents { get; set; }
    }
}
