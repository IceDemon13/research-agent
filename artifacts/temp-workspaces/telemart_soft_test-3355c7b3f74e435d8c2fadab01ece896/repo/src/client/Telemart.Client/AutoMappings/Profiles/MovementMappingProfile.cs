using System.Collections.Generic;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ReportDesigner;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.MovementReport;
using Telemart.Client.ViewModels.Warehouse.Movement;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class MovementMappingProfile : Profile
    {
        public MovementMappingProfile()
        {
            CreateMap<MovementDto, MovementViewItem>()
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<MovementState>, int>(z => z.StateId))
                .ForMember(x => x.Purposes, y => y.MapFrom<DictionaryItemsToObservableCollectionValueResolver<WarehouseRouteTimePurpose>, List<int>>(z => z.PurposeIds))
                .ForMember(x => x.Carry, y => y.MapFrom<DictionaryItemValueResolver<CarryType>, int>(z => z.CarryId ?? 0));
            CreateMap<MovementProductDto, MovementProductViewItem>()
                .ForMember(x => x.ProductParentCategoryId, x => x.Ignore())
                .ForMember(x => x.AdditionalServicesQuantity, x => x.Ignore())
                .ForMember(x => x.AdditionalServiceProductToolTip, x => x.Ignore())
                .ForMember(x => x.ProductParentCategoryName, x => x.Ignore())
                .ForMember(x => x.ProductParentCategoryLeft, x => x.Ignore())
                .ForMember(x => x.GroupString, x => x.Ignore())
                .ForMember(x => x.AdditionalServiceProductId, x => x.Ignore())
                .ForMember(x => x.ManualAdded, x => x.Ignore())
                .ForMember(x => x.AssembledComputerForOrder, x => x.Ignore())
                .ForMember(x => x.AssembledComputerForOrderToolTip, x => x.Ignore())
                .ForMember(x => x.RowRef, y => y.Ignore())
                .ForMember(x => x.ParentRowRef, y => y.Ignore())
                .ForMember(x => x.AssemblyServiceId, y => y.Ignore())
                .ForMember(x => x.AssemblyServiceProductId, y => y.Ignore())
                .ForMember(x => x.AdditionalServiceProductSerialNumber, y => y.Ignore())
                .ForMember(x => x.PrimaryAdditionalServiceProductSerialNumber, y => y.Ignore())
                .ForMember(x => x.ProductNameBase, x => x.MapFrom(z => z.ProductName))
                .Ignore(x => x.IsConsumableAdditionalServiceProduct)
                .Ignore(x => x.OrderIds);
            CreateMap<MovementProductSnDto, MovementProductSnViewItem>()
                .ForMember(x => x.NotSaved, x => x.Ignore());

            CreateMap<MovementProductDto, MovementDelayProductViewItem>()
                .ForMember(x => x.AlternativeId, x => x.Ignore())
                .ForMember(x => x.RemoveSource, x => x.Ignore())
                .ForMember(x => x.ProductId, y => y.MapFrom(z => z.ProductId))
                .ForMember(x => x.FullName, y => y.MapFrom(z => z.ProductName.GetStringWithPrefix(z.ProductPrefixRus)))
                .ForMember(x => x.FullNameUkr, y => y.MapFrom(z => z.ProductNameUa.GetStringWithPrefix(z.ProductPrefixUa)))
                .ForMember(x => x.FullNameEn, y => y.MapFrom(z => z.ProductNameEn.GetStringWithPrefix(z.ProductPrefixEn)))
                .ForMember(x => x.Id, y => y.Ignore())
                .ForMember(x => x.OrderQuantity, y => y.Ignore());

            CreateMap<MovementReportProductDto, MovementProductReportData>()
                .ForMember(x => x.ProductName, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.PartBarcode, y => y.MapFrom(z => z.Barcode.GetLastPartSubString(4)))
                .ForMember(x => x.ProductsCount, y => y.MapFrom(z => z.Quantity));
        }
    }
}