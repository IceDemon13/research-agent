using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Cell;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;
using Telemart.Client.TransferObjects.Warehouse.Route;
using Telemart.Client.ViewModels.Locations;
using Telemart.Client.ViewModels.Warehouse;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class WarehouseMappingProfile : Profile
    {
        public WarehouseMappingProfile()
        {
            CreateMap<WarehouseDto, WarehouseSimpleDto>();

            CreateMap<WarehouseDto, WarehouseViewItem>()
                .ForMember(x => x.Employee, x => x.Ignore())
                .ForMember(x => x.EmployeeAssembly, x => x.Ignore())
                .ForMember(x => x.EmployeeAdditionalService, x => x.Ignore())
                .ForMember(x => x.BufferWarehouse, x => x.Ignore())
                .ForMember(x => x.AssemblyWarehouse, x => x.Ignore())
                .ForMember(x => x.IsActive, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.Name));

            CreateMap<WarehouseViewItem, WarehouseSaveDto>();

            CreateMap<WarehouseCreateViewModel, WarehouseCreateDto>()
                .ForMember(x => x.EmployeeAdditionalServiceId, x => x.Ignore());

            CreateMap<WarehouseRouteSimpleDto, WarehouseRouteViewItem>()
                .ForMember(x => x.WarehouseFrom, x => x.Ignore())
                .ForMember(x => x.WarehouseTo, x => x.Ignore());

            CreateMap<WarehouseRouteTimeDto, WarehouseRouteTimeViewItem>()
                .ForMember(x => x.Purposes, x => x.Ignore())
                .ForMember(x => x.DeliveryTypeEnabled, x => x.Ignore())
                .ForMember(x => x.TimeIn, y => y.MapFrom(z => default(DateTime) + z.TimeIn))
                .ForMember(x => x.TimeOut, y => y.MapFrom(z => default(DateTime) + z.TimeOut))
                .ForMember(x => x.TimeArrive, y => y.MapFrom(z => default(DateTime) + z.TimeArrive))
                .ForMember(x => x.TimeDeparture, y => y.MapFrom(z => default(DateTime) + z.TimeDeparture));

            CreateMap<WarehouseRouteTimeViewItem, WarehouseRouteTimeDto>()
                .ForMember(x => x.PurposeIds, x => x.MapFrom(z => z.Purposes.Cast<WarehouseRouteTimePurpose>().Select(s => s.Id)))
                .ForMember(x => x.TimeIn, y => y.MapFrom(z => z.TimeIn.TimeOfDay))
                .ForMember(x => x.DeliveryTypeId, y => y.MapFrom(z => z.DeliveryType == null ? (int?)null : z.DeliveryType.Id))
                .ForMember(x => x.TimeOut, y => y.MapFrom(z => z.TimeOut.TimeOfDay))
                .ForMember(x => x.TimeArrive, y => y.MapFrom(z => z.TimeArrive.TimeOfDay))
                .ForMember(x => x.TimeDeparture, y => y.MapFrom(z => z.TimeDeparture.TimeOfDay));

            CreateMap<DeliveryDto, WarehouseDeliveryViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.CarriesString, x => x.Ignore())
                .ForMember(x => x.CarryIds, y => y.MapFrom(z => new ObservableCollection<int>(z.CarryIds)))
                .ForMember(x => x.EntityTypeIds, y => y.MapFrom(z => new ObservableCollection<int>(z.EntityTypeIds)));

            CreateMap<WarehouseDeliveryViewItem, DeliverySaveDto>()
                .ForMember(x => x.CarryIds, y => y.MapFrom(z => z.CarryIds.ToArray()))
                .ForMember(x => x.EntityTypeIds, y => y.MapFrom(z => z.EntityTypeIds.ToArray()));

            CreateMap<WarehousePerfomanceEditParameter, WarehousePerfomanceViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<WarehousePerformanceDto, WarehousePerfomanceViewItem>()
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.EmployeeLockId, x => x.Ignore());

            CreateMap<WarehouseDeliveryEditParameter, WarehouseDeliveryViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate())
                .ForMember(x => x.CarryIds, y => y.MapFrom(z => new ObservableCollection<int>()))
                .ForMember(x => x.EntityTypeIds, y => y.MapFrom(z => new ObservableCollection<int>()));

            CreateMap<WarehouseCellDto, WarehouseCellViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());

            CreateMap<WarehouseCellParameter, WarehouseCellViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<LocationEntityDto, LocationViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());

            CreateMap<ClusterDto, ClusterViewItem>()
                .ForMember(x => x.Locations, y => y.Ignore())
                .ForMember(x => x.EmployeeLockId, y => y.Ignore())
                .ForMember(x => x.EmployeeLockName, y => y.Ignore());

            CreateMap<ClusterViewItem, ClusterDto>()
                .ForMember(x => x.LocationIds, y => y.MapFrom(x => x.GetLocationIds()));

            CreateMap<ClusterParameter, ClusterViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate())
                .ForMember(x => x.EmployeeLockId, y => y.Ignore())
                .ForMember(x => x.EmployeeLockName, y => y.Ignore());
        }
    }
}