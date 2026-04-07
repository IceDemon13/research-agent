using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Discussions
{
    public class DiscussionViewItem : TelemartEditorViewItemBase
    {
        public int? StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int? TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int? PriorityId
        {
            get { return GetProperty(() => PriorityId); }
            set { SetProperty(() => PriorityId, value); }
        }

        public DateTime? Deadline
        {
            get { return GetProperty(() => Deadline); }
            set { SetProperty(() => Deadline, value); }
        }

        public string BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public string Title
        {
            get { return GetProperty(() => Title); }
            set { SetProperty(() => Title, value); }
        }

        public string Body
        {
            get { return GetProperty(() => Body); }
            set { SetProperty(() => Body, value); }
        }

        public string FormatBody
        {
            get { return GetProperty(() => FormatBody); }
            set { SetProperty(() => FormatBody, value); }
        }

        public int? ExecutorEmployeeId
        {
            get { return GetProperty(() => ExecutorEmployeeId); }
            set { SetProperty(() => ExecutorEmployeeId, value); }
        }

        public bool PriorityBitrix
        {
            get { return GetProperty(() => PriorityBitrix); }
            set { SetProperty(() => PriorityBitrix, value); }
        }

        public bool TaskControlBitrix
        {
            get { return GetProperty(() => TaskControlBitrix); }
            set { SetProperty(() => TaskControlBitrix, value); }
        }

        public bool UseBitrixGroupFromExecutorDepartment
        {
            get { return GetProperty(() => UseBitrixGroupFromExecutorDepartment); }
            set { SetProperty(() => UseBitrixGroupFromExecutorDepartment, value); }
        }

        public ObservableCollection<int> HashtagIds
        {
            get { return GetProperty(() => HashtagIds); }
            set { SetProperty(() => HashtagIds, value); }
        }

        public ObservableCollection<int> CoExecutorEmployeeIds
        {
            get { return GetProperty(() => CoExecutorEmployeeIds); }
            set { SetProperty(() => CoExecutorEmployeeIds, value); }
        }

        public ObservableCollection<int> AuditorEmployeeIds
        {
            get { return GetProperty(() => AuditorEmployeeIds); }
            set { SetProperty(() => AuditorEmployeeIds, value); }
        }

        public int? CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime? CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int? ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime? ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public bool ClientLogs
        {
            get { return GetProperty(() => ClientLogs); }
            set { SetProperty(() => ClientLogs, value); }
        }

        public ObservableCollection<DiscussionEntityDocumentViewItem> EntityDocuments
        {
            get { return GetProperty(() => EntityDocuments); }
            set { SetProperty(() => EntityDocuments, value); }
        }

        public static void BuildMetadata(MetadataBuilder<DiscussionViewItem> builder)
        {
            builder.Property(x => x.PriorityId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TypeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ExecutorEmployeeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Body)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(1000, () => "Длина не должна превышать 1000 символов");
            builder.Property(x => x.Title)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(150, () => "Длина заголовка не должна превышать 150 символов");
        }

        public override object Clone()
        {
            DiscussionViewItem item = (DiscussionViewItem)base.Clone();

            item.EntityDocuments = EntityDocuments?
                    .Select(x => ReflectionObjectCloner.Clone(x))
                    .ToObservableCollection();

            return item;
        }
    }
}