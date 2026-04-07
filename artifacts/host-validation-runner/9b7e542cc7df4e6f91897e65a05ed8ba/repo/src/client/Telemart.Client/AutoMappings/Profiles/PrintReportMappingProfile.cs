using AutoMapper;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Extensions;
using Telemart.Client.ReportDesigner.Order;
using Telemart.Client.Reports.Order;
using Telemart.Client.Reports.Order.Assembly;
using Telemart.Client.Reports.ReturnInvoice;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Assembly;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class PrintReportMappingProfile : Profile
    {
        public PrintReportMappingProfile()
        {
            CreateMap<ReturnInvoiceReportDataDto, ReturnInvoiceReportData>()
                .ForMember(x => x.ReturnInvoice, x => x.Ignore())
                .ForMember(x => x.Invoice, x => x.Ignore());
            CreateMap<ReturnInvoiceProductReportDataDto, ReturnInvoiceProductReportData>()
                .ForMember(x => x.Id, x => x.Ignore());
            CreateMap<OrderAssemblyProductReportDataDto, OrderAssemblyProductReportData>();
            CreateMap<OrderAssemblyReportDataDto, OrderAssemblyReportData>()
                .Ignore(x => x.DateTimeAssemblyPrint);
            CreateMap<OrderAssemblyReportSimpleDataDto, OrderAssemblyReportSimpleData>()
                .Ignore(x => x.DateTimeAssemblyPrint);
            CreateMap<PackListPrintProductDto, OrderPackListProductReportData>()
                .ForMember(x => x.PartBarcode, y => y.MapFrom(z => z.Barcode.GetLastPartSubString(4)));
            CreateMap<AssemblySheetReportDataDto, OrderSingleAssemblyReportData>()
                .Ignore(x => x.DateTimeAssemblyPrint)
                .Ignore(x => x.AssemblyAdditionalServiceProducts);
        }
    }
}