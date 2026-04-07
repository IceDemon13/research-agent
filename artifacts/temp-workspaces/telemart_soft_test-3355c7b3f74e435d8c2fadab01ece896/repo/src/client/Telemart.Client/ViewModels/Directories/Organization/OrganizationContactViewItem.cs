using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Directories.Organization
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class OrganizationContactViewItem : ICloneable
    {
        protected OrganizationContactViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int OrganizationId { get; set; }

        public virtual int? EmployeeId { get; set; }

        public virtual EmployeeSimpleDto Employee { get; set; }

        public virtual int PositionId { get; set; }

        public virtual OrganizationPosition Position { get; set; }

        public virtual bool IsDefault { get; set; }

        public static OrganizationContactViewItem Create()
        {
            return ViewModelSource<OrganizationContactViewItem>.Create();
        }

        public static void BuildMetadata(MetadataBuilder<OrganizationContactViewItem> builder)
        {
            builder.Property(x => x.EmployeeId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Position)
                .Required(() => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public OrganizationContactViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this, Create);
        }

        protected void OnPositionChanged(OrganizationPosition oldPosition)
        {
            if (Position != null)
            {
                PositionId = Position.Id;
            }
        }
    }
}
