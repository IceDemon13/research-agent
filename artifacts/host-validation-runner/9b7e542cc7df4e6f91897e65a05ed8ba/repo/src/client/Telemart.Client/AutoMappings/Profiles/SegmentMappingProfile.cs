using AutoMapper;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects.Segment;
using Telemart.Client.TransferObjects.TradeInSegment;
using Telemart.Client.ViewModels.Segment;
using Telemart.Client.ViewModels.TradeInSegment;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class SegmentMappingProfile : Profile
    {
        public SegmentMappingProfile()
        {
            CreateMap<SegmentDto, SegmentViewItem>()
                .ForMember(x => x.FeatureString, x => x.Ignore())
                .ForMember(x => x.AnyNewFeatures, x => x.Ignore())
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.CategoryFeatures, x => x.MapFrom(z => z.GetSegmentCategoryFeatures()));

            CreateMap<SegmentCategoryFeatureDto, SegmentCategoryFeatureViewItem>()
                .ForMember(x => x.FeatureValues, x => x.Ignore())
                .ForMember(x => x.DisplayFeatureValues, x => x.Ignore());

            CreateMap<SegmentCategorySettingsDto, SegmentCategorySettingsViewItem>()
                .ForMember(x => x.AllowEdit, x => x.Ignore())
                .ForMember(x => x.Features, x => x.Ignore())
                .ForMember(x => x.DisplayFeatures, x => x.Ignore());

            CreateMap<SegmentParameter, SegmentViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<TradeInSegmentDto, TradeInSegmentViewItem>()
                .ForMember(x => x.FeatureString, x => x.Ignore())
                .ForMember(x => x.AnyNewFeatures, x => x.Ignore())
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.CategoryFeatures, x => x.MapFrom(z => z.GetSegmentCategoryFeatures()));

            CreateMap<TradeInSegmentCategoryFeatureDto, TradeInSegmentCategoryFeatureViewItem>()
                .ForMember(x => x.FeatureValues, x => x.Ignore())
                .ForMember(x => x.DisplayFeatureValues, x => x.Ignore());

            CreateMap<TradeInSegmentCategorySettingsDto, TradeInSegmentCategorySettingsViewItem>()
                .ForMember(x => x.AllowEdit, x => x.Ignore())
                .ForMember(x => x.Features, x => x.Ignore())
                .ForMember(x => x.DisplayFeatures, x => x.Ignore());

            CreateMap<TradeInSegmentParameter, TradeInSegmentViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
        }
    }
}