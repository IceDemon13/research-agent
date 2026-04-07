using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.ViewModels.CompanyStructure
{
    public sealed class StructureDiagramViewItem
    {
        public StructureDiagramViewItem()
        {
        }

        public StructureDiagramViewItem(
            int departmentId,
            string departmentName,
            int? mainEmployeeId,
            string mainEmployeeName,
            string mainEmployeePosition,
            string email,
            string phone,
            int parentId)
        {
            DepartmentId = departmentId;
            DepartmentName = departmentName;
            MainEmployeeId = mainEmployeeId;
            MainEmployeeName = mainEmployeeName;
            MainEmployeePosition = mainEmployeePosition;
            Email = email;
            Phone = phone;
            ParentId = parentId;
        }

        [ReadOnly(true)]
        [Display(AutoGenerateField = false)]
        public int DepartmentId { get; set; }

        [Display(GroupName = "General Info")]
        [Required]
        [MaxLength(50, ErrorMessage = "Value is too long")]
        public string DepartmentName { get; set; }

        [Display(GroupName = "General Info")]
        public int? MainEmployeeId { get; set; }

        [Display(GroupName = "General Info")]
        [DisplayFormat(NullDisplayText = "<empty>")]
        public string MainEmployeeName { get; set; }

        [Display(GroupName = "General Info")]
        [DisplayFormat(NullDisplayText = "<empty>")]
        public string MainEmployeePosition { get; set; }

        [Display(GroupName = "Contacts")]
        [DisplayFormat(NullDisplayText = "<empty>")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(GroupName = "Contacts")]
        [DataType(DataType.PhoneNumber)]
        [DisplayFormat(NullDisplayText = "<empty>")]
        public string Phone { get; set; }

        [ReadOnly(true)]
        [Display(AutoGenerateField = false)]
        public int ParentId { get; set; }
    }
}