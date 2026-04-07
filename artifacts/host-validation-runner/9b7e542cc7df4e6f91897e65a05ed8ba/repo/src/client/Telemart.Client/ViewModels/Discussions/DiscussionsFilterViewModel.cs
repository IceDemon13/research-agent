using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Discussions
{
    public class DiscussionsFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        public DiscussionsFilterViewModel()
        {
            InitStateIds();
        }

        public string Error => string.Empty;

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public string Ids
        {
            get { return GetProperty(() => Ids); }
            set { SetProperty(() => Ids, value); }
        }

        public int? ExecutorEmployeeId
        {
            get { return GetProperty(() => ExecutorEmployeeId); }
            set { SetProperty(() => ExecutorEmployeeId, value); }
        }

        public int? CoExecutorEmployeeId
        {
            get { return GetProperty(() => CoExecutorEmployeeId); }
            set { SetProperty(() => CoExecutorEmployeeId, value); }
        }

        public int? AuditorEmployeeId
        {
            get { return GetProperty(() => AuditorEmployeeId); }
            set { SetProperty(() => AuditorEmployeeId, value); }
        }

        public int? CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public string Title
        {
            get { return GetProperty(() => Title); }
            set { SetProperty(() => Title, value); }
        }

        public string BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public int? EntityId
        {
            get { return GetProperty(() => EntityId); }
            set { SetProperty(() => EntityId, value); }
        }

        public DateTime? DeadlineBefore
        {
            get { return GetProperty(() => DeadlineBefore); }
            set { SetProperty(() => DeadlineBefore, value); }
        }

        public DateTime? DeadlineAfter
        {
            get { return GetProperty(() => DeadlineAfter); }
            set { SetProperty(() => DeadlineAfter, value); }
        }

        public DateTime? CreatedOnBefore
        {
            get { return GetProperty(() => CreatedOnBefore); }
            set { SetProperty(() => CreatedOnBefore, value); }
        }

        public DateTime? CreatedOnAfter
        {
            get { return GetProperty(() => CreatedOnAfter); }
            set { SetProperty(() => CreatedOnAfter, value); }
        }

        public ObservableCollection<int> StateIds
        {
            get { return GetProperty(() => StateIds); }
            set { SetProperty(() => StateIds, value); }
        }

        public DiscussionsFilteringItem GetFilteringItem()
        {
            return new DiscussionsFilteringItem()
            {
                CreatedBy = CreatedBy,
                DeadlineAfter = DeadlineAfter,
                DeadlineBefore = DeadlineBefore,
                CreatedOnAfter = CreatedOnAfter,
                CreatedOnBefore = CreatedOnBefore,
                DocumentId = DocumentId,
                EntityId = EntityId,
                ExecutorEmployeeId = ExecutorEmployeeId,
                CoExecutorEmployeeId = CoExecutorEmployeeId,
                AuditorEmployeeId = AuditorEmployeeId,
                StateIds = StateIds?.ToArray(),
                BitrixId = BitrixId,
                Title = Title,
                Ids = Ids
            };
        }

        public void Reset()
        {
            CreatedOnAfter = null;
            CreatedOnBefore = null;
            DeadlineAfter = null;
            DeadlineBefore = null;
            CreatedBy = null;
            ExecutorEmployeeId = null;
            DocumentId = null;
            EntityId = null;
            BitrixId = null;
            Title = null;
            CoExecutorEmployeeId = null;
            AuditorEmployeeId = null;
            Ids = null;
            InitStateIds();
        }


        private void InitStateIds()
        {
            StateIds = new[]
            {
                DiscussionState.PendingExecutionId,
                DiscussionState.ExecutionId,
                DiscussionState.PendingApprovalId,
                DiscussionState.PostponedId
            }.ToObservableCollection();
        }
    }
}