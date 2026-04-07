using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;
using Telemart.Client.Data.Requests.Features.ReportLayout.TransferObjects;
using Telemart.Client.ViewModels.Reporting;
using Telemart.Client.ViewModels.Reporting.Mvvm;
using Telemart.Client.ViewModels.Reporting.ViewItems;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ReportMappingProfile : Profile
    {
        public ReportMappingProfile()
        {
            CreateMap<ReportDto, ReportSimpleViewItem>();
            CreateMap<ReportSimpleDto, ReportSimpleViewItem>();
            CreateMap<ReportDto, ReportViewItem>()
                .ForMember(x => x.Roles, y => y.MapFrom(z => z.Roles.ToObservableCollection()))
                .ForMember(x => x.Employees, y => y.MapFrom(z => z.Employees.ToObservableCollection()))
                .ForMember(x => x.EmployeeLockId, y => y.Ignore())
                .ForMember(x => x.EmployeeLockName, y => y.Ignore());

            CreateMap<ReportParameterDto, ReportParameterViewItem>()
                .ForMember(x => x.EditorType, y => y.MapFrom(z => (ReportParameterEditorType)z.EditorType));

            CreateMap<ReportFieldDto, ReportFieldViewItem>()
                .ForMember(x => x.UniqueName, y => y.MapFrom(z => z.UniqueName ?? z.FieldName));

            CreateMap<ReportViewItem, ReportSaveDto>()
                .ForMember(x => x.Roles, y => y.MapFrom(z => z.Roles.ToArray()))
                .ForMember(x => x.Employees, y => y.MapFrom(z => z.Employees.ToArray()));

            CreateMap<ReportParameterViewItem, ReportParameterDto>()
                .ForMember(x => x.EditorType, y => y.MapFrom(z => (int)z.EditorType));

            CreateMap<ReportFieldViewItem, ReportFieldDto>();

            CreateMap<DataSourceDto, DataSourceViewItem>()
                .ConstructUsing((source, dest) => DataSourceViewItem.Create());

            CreateMap<ReportLayoutDto, ReportLayoutViewItem>()
                .ForMember(x => x.View, y => y.MapFrom(z => (ReportLayoutType)z.View))
                .ConstructUsing((source, dest) => ReportLayoutViewItem.Create());

            CreateMap<ReportViewMessage, ReportViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate())
                .AfterMap((source, destination) =>
                {
                    destination.Parameters = new ObservableCollection<ReportParameterViewItem>();
                    destination.Fields = new ObservableCollection<ReportFieldViewItem>();
                    destination.Roles = new ObservableCollection<string>();
                    destination.Employees = new ObservableCollection<int>();
                });
        }
    }
}