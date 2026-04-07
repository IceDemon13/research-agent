using System.Collections.ObjectModel;
using AutoMapper;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Content.ProductFeatureGroups;
using Telemart.Client.ViewModels.Content.ProductFeatureGroups.FeatureValues;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class FeatureMappingProfile : Profile
    {
        public FeatureMappingProfile()
        {
            CreateMap<FeatureFullDto, FeatureViewItem>()
                .ForMember(x => x.CustomRegex, x => x.Ignore())
                .ForMember(x => x.FullNameIcon, x => x.Ignore())
                .ForMember(x => x.IsMultiValue, y => y.MapFrom(z => z.Separator != null));
            CreateMap<FeatureViewItem, FeatureSaveDto>()
                .ForMember(x => x.ChangeImage, x => x.Ignore())
                .ForMember(x => x.IconBytes, x => x.Ignore());
            CreateMap<FeatureViewParameter, FeatureViewItem>(MemberList.Source)
                .ForSourceMember(x => x.CategoryId, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<FeatureGroupSimpleDto, FeatureGroupSimpleViewItem>()
                .AfterMap((x, y) => y.Name = y.Name.Trim());

            CreateMap<FeatureContractorKeyDto, FeatureContractorKeyViewItem>();
            CreateMap<FeatureContractorKeyViewItem, FeatureContractorKeyDto>()
                .ForMember(x => x.FeatureId, x => x.Ignore());

            CreateMap<FeatureGroupSimpleDto, FeatureGroupViewItem>()
                .ForMember(x => x.Features, x => x.Ignore())
                .AfterMap((x, y) => y.Name = y.Name.Trim())
                .AfterMap((x, y) => y.Features ??= new ObservableCollection<FeatureViewItem>());

            CreateMap<FeatureGroupDto, FeatureGroupViewItem>()
                .ForMember(x => x.Features, y => y.Ignore())
                .AfterMap((x, y) => y.Name = y.Name.Trim());

            CreateMap<FeatureValueExDto, FeatureValueViewItem>()
                .ForMember(x => x.DisplayValue, x => x.Ignore())
                .ConstructUsing(x => new FeatureValueViewItem(x.Id, x.FeatureId, x.Value, x.ValueUkr, x.ValueEn, x.Url, x.UrlUkr, x.UrlEn, x.Regex, x.MultiLanguage));

            CreateMap<FeatureValueDto, ComboBoxItem>()
                .ConstructUsing(x => new ComboBoxItem(x.Id, x.Value, true, null));

            CreateMap<FeatureValueViewItem, FeatureValueSaveDto>()
                .ForMember(x => x.Value, y => y.MapFrom(z => z.Value.Trim()))
                .ForMember(x => x.Url, y => y.MapFrom(z => z.Url.Trim()));

            CreateMap<FeatureSimpleDto, HierarchicalItem>()
                .ConstructUsing(x => new HierarchicalItem(x.Id, string.IsNullOrWhiteSpace(x.Name) ? string.Empty : x.Name.Trim() ?? string.Empty, x.GroupId, true));
        }
    }
}