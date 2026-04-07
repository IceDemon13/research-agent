using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.ParserSettings;
using Telemart.Client.ViewModels.Directories.Contractor.ParserSettings;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ParserSettingsMappingProfile : Profile
    {
        public ParserSettingsMappingProfile()
        {
            CreateMap<ParserSettingsCategoryReplaceDto, ParserSettingsCategoryReplaceViewItem>();
            CreateMap<ParserSettingsCategoryDto, ParserSettingsCategoryViewItem>()
                .ForMember(x => x.CategoryIds, y => y.MapFrom(z => z.Categories.ToObservableCollection()))
                .Ignore(x => x.Categories);
            CreateMap<ParserSettingsAvailabilityDto, ParserSettingsAvailabilityViewItem>();
            CreateMap<ParserSettingsDto, ParserSettingsViewItem>()
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.Name))
                .ForMember(x => x.ParserSettingsFile, x => x.MapFrom(z => z.ParserSettingsFile ?? new ParserSettingsFileDto()));
            CreateMap<ParserSettingsParameter, ParserSettingsViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
            CreateMap<ParserSettingsFileViewItem, ParserSettingsFileDto>();
            CreateMap<ParserSettingsFileDto, ParserSettingsFileViewItem>()
                .ForMember(x => x.FileColumn, x => x.MapFrom(z => z.FileColumn ?? new ParserSettingsFileColumnDto()));
            CreateMap<ParserSettingsFilePriceDto, ParserSettingsFilePriceViewItem>();
            CreateMap<ParserSettingsFilePriceViewItem, ParserSettingsFilePriceDto>();

            CreateMap<ParserSettingsFileColumnCategoryRuleDto, ParserSettingsFileColumnCategoryRuleViewItem>();
            CreateMap<ParserSettingsFileColumnCategoryRuleViewItem, ParserSettingsFileColumnCategoryRuleDto>();
            CreateMap<ParserSettingsFileColumnViewItem, ParserSettingsFileColumnDto>();
            CreateMap<ParserSettingsFileColumnDto, ParserSettingsFileColumnViewItem>()
                .ForMember(x => x.CategoryRule1, x => x.MapFrom(z => z.CategoryRule1 ?? new ParserSettingsFileColumnCategoryRuleDto()))
                .ForMember(x => x.CategoryRule2, x => x.MapFrom(z => z.CategoryRule2 ?? new ParserSettingsFileColumnCategoryRuleDto()))
                .ForMember(x => x.CategoryRule3, x => x.MapFrom(z => z.CategoryRule3 ?? new ParserSettingsFileColumnCategoryRuleDto()))
                .ForMember(x => x.Prices, x => x.MapFrom(z => z.Prices ?? new ObservableCollection<ParserSettingsFilePriceDto>()));

            CreateMap<ParserSettingsCategoryReplaceViewItem, ParserSettingsCategoryReplaceDto>()
                .ForMember(x => x.To, y => y.MapFrom(z => z.To ?? string.Empty));
            CreateMap<ParserSettingsCategoryViewItem, ParserSettingsCategoryDto>()
                .ForMember(x => x.Categories, y => y.MapFrom(z => z.Categories.Select(i => i.Id)));
            CreateMap<ParserSettingsAvailabilityViewItem, ParserSettingsAvailabilityDto>();
            CreateMap<ParserSettingsViewItem, ParserSettingsDto>()
                .ForMember(x => x.EmployeeLock, x => x.Ignore());
        }
    }
}