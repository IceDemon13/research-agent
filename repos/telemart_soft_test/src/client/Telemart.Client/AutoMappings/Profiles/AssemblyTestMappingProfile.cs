using AutoMapper;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.AssemblyTest;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class AssemblyTestMappingProfile : Profile
    {
        public AssemblyTestMappingProfile()
        {
            CreateMap<AssemblyTestGroupParameter, AssemblyTestGroupSimpleViewItem>(MemberList.Source)
                .ForSourceMember(x => x.ParentName, x => x.DoNotValidate())
                .ForSourceMember(x => x.ParentId, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<AssemblyTestGroupDto, AssemblyTestGroupSimpleViewItem>()
                .ForMember(x => x.ShowInAssemblyServiceTestGrid, x => x.Ignore());
            CreateMap<AssemblyTestGroupDto, AssemblyTestGroupViewItem>()
                .ForMember(x => x.Groups, x => x.Ignore())
                .ForMember(x => x.Children, x => x.Ignore())
                .ForMember(x => x.ShowInAssemblyServiceTestGrid, x => x.Ignore());
            CreateMap<AssemblyTestDto, AssemblyTestsViewItem>()
                .ForMember(x => x.Value, x => x.Ignore())
                .ForMember(x => x.OriginalValue, x => x.Ignore());

            CreateMap<AssemblyTestDto, AssemblyTestViewItem>();
            CreateMap<AssemblyTestParameter, AssemblyTestViewItem>(MemberList.Source)
                .ForSourceMember(x => x.GroupId, x => x.DoNotValidate())
                .ForSourceMember(x => x.GroupName, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<AssemblyTestViewItem, AssemblyTestCreateDto>();
            CreateMap<AssemblyTestSlaveCategoryViewItem, AssemblyTestSlaveCategoryCreateDto>();
            CreateMap<AssemblyTestSlaveCategoryDto, AssemblyTestSlaveCategoryViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());
        }
    }
}