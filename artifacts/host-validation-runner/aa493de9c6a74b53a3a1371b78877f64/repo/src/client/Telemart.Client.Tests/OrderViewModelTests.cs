using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Telemart.Client.AutoMappings.Profiles;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Hashtag.Actions;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.OrderDocuments;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.Requests.Features.Promo;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.Requests.Features.Warehouse.Performance;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Promo;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class OrderViewModelTests
    {
        [Fact]
        public async Task InitializeAddTestAsync()
        {
            MockRepository repository = new MockRepository(MockBehavior.Loose);

            Mock<IWebClient> webClientMock = repository.Create<IWebClient>(MockBehavior.Strict);

            webClientMock.Setup(x => x.IsOperationAllowed(It.IsAny<BusinessOperation>())).Returns(true).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryEmployees>(), It.IsAny<bool>())).ReturnsAsync(GetEmployees()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryContractors>(), It.IsAny<bool>())).ReturnsAsync(GetContractors()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryCities>(), It.IsAny<bool>())).ReturnsAsync(GetCities()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryWarehouses>(), It.IsAny<bool>())).ReturnsAsync(GetWarehouses()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryWarehouseDeliveries>(), It.IsAny<bool>())).ReturnsAsync(GetWarehouseDeliveries()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryAllPerformances>(), It.IsAny<bool>())).ReturnsAsync(GetWarehousePerformances()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryOrganizations>(), It.IsAny<bool>())).ReturnsAsync(GetOrganizations()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryCashboxes>(), It.IsAny<bool>())).ReturnsAsync(GetCashboxes()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryAssemblyServices>(), It.IsAny<bool>())).ReturnsAsync(GetAssemblyServices()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryAdditionalServiceProducts>(), It.IsAny<bool>())).ReturnsAsync(GetAdditionalServiceProducts()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryAdditionalServices>(), It.IsAny<bool>())).ReturnsAsync(GetAdditionalServices()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryPromos>(), It.IsAny<bool>())).ReturnsAsync(GetPromos()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryHashtagsByPhone>(), It.IsAny<bool>())).ReturnsAsync(GetHashtagsByPhone()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryOrderStateChangeReasons>(), It.IsAny<bool>())).ReturnsAsync(GetOrderStateChangeReasons()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryLegalEntities>(), It.IsAny<bool>())).ReturnsAsync(GetLegalEntities()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryOrderDocuments>(), It.IsAny<bool>())).ReturnsAsync(GetOrderDocuments()).Verifiable();
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryOrderDocumentTypes>(), It.IsAny<bool>())).ReturnsAsync(GetOrderDocumentTypes()).Verifiable();
            webClientMock.SetupGet(x => x.AuthenticatedEmployee).Returns(new EmployeeContextDto { Id = 1, Roles = new List<string> { Role.Admin.Name } }).Verifiable();

            IDictionaries dictionaries = new Dictionaries.Dictionaries(null);
            IOrderRules orderRules = new OrderRules(dictionaries, webClientMock.Object);
            Mock<IMessageFacadeService> messageFacadeServiceMock = repository.Create<IMessageFacadeService>();
            Mock<IMessenger> messengerMock = repository.Create<IMessenger>();
            Mock<IMediator> mediatorMock = repository.Create<IMediator>();
            Mock<IPrintingSettingsStore> printingSettingsMock = repository.Create<IPrintingSettingsStore>();
            Mock<IFiscalRegistrarClientFactory> fiscalRegistrarFactoryMock = repository.Create<IFiscalRegistrarClientFactory>();
            Mock<IErrorHandler> errorHandlerMock = repository.Create<IErrorHandler>();
            Mock<IOrderGiveHelper> orderGiveHelperMock = repository.Create<IOrderGiveHelper>();
            Mock<IRroPrintHelper> rroPrintHelper = repository.Create<IRroPrintHelper>();
            Mock<IDictionaries> dictionariesMock = repository.Create<IDictionaries>();
            Mock<IPrintingSettingsStore> printingSettingsStoreMock = repository.Create<IPrintingSettingsStore>();
            Mock<ILockableOperationProcessorFactory> lockableOperationProcessorFactoryMock = repository.Create<ILockableOperationProcessorFactory>();
            Mock<ILogger<OrderViewModel>> loggerMock = repository.Create<ILogger<OrderViewModel>>();
            Mock<ProductInformationViewModel> productInformationMock = repository.Create<ProductInformationViewModel>();

            MapperConfiguration mapperConfiguration = new MapperConfiguration(conf =>
            {
                conf.AddProfile(new OrderMappingProfile());
            });

            OrderViewModel vm = new(
                webClientMock.Object,
                dictionaries,
                orderRules,
                mapperConfiguration.CreateMapper(),
                messengerMock.Object,
                messageFacadeServiceMock.Object,
                mediatorMock.Object,
                printingSettingsMock.Object,
                orderGiveHelperMock.Object,
                rroPrintHelper.Object,
                new DocumentCommands(
                    messageFacadeServiceMock.Object,
                    messengerMock.Object,
                    webClientMock.Object,
                    dictionariesMock.Object,
                    printingSettingsStoreMock.Object,
                    errorHandlerMock.Object,
                    mediatorMock.Object),
                new TelegramBotOptions(),
                fiscalRegistrarFactoryMock.Object,
                errorHandlerMock.Object,
                lockableOperationProcessorFactoryMock.Object,
                loggerMock.Object,
                productInformationMock.Object,
                new NovaPayOptions());

            OrderDto order = new OrderDto
            {
                Products = new List<OrderProductDto>(),
                PromoCodes = new List<OrderPromoCodeDto>(),
                OrderPayments = new List<OrderPaymentDto>()
            };

            await vm.InitializeAddAsync(order);

            webClientMock.VerifyAll();
        }

        [Fact]
        public void IsInheritedFromIDataErrorInfoTest()
        {
            Type[] interfaces = typeof(OrderViewModel).GetInterfaces();
            Assert.Contains(typeof(IDataErrorInfo), interfaces);
        }

        [Fact]
        public void IsInheritedFromIDocumentContentTest()
        {
            Type[] interfaces = typeof(OrderViewModel).GetInterfaces();
            Assert.Contains(typeof(IDocumentContent), interfaces);
        }

        [Fact]
        public void IsViewModelTest()
        {
            Assert.True(typeof(OrderViewModel).BaseType == typeof(ViewModelBase));
        }

        private static List<T> GenerateTestRecords<T>(Func<int, T> createFunc, int count)
        {
            List<T> records = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                int id = i + 1;
                T record = createFunc(id);
                records.Add(record);
            }

            return records;
        }

        private static PagedResult<T> GeneratePagedTestRecords<T>(Func<int, T> createFunc, int count)
            where T : new()
        {
            List<T> records = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                int id = i + 1;
                T record = createFunc(id);
                records.Add(record);
            }

            return new PagedResult<T>() { Data = records };
        }

        private static PagedResult<CityDto> GetCities()
        {
            return GeneratePagedTestRecords(CreateTestRecord, 20);

            CityDto CreateTestRecord(int id)
            {
                return new CityDto { Id = id, Name = $"City{id}", Position = id };
            }
        }

        private static PagedResult<ContractorDto> GetContractors()
        {
            return new PagedResult<ContractorDto> { Data = GenerateTestRecords(CreateTestRecord, 5) };

            ContractorDto CreateTestRecord(int id)
            {
                return new ContractorDto
                {
                    Id = id,
                    Active = true,
                    Name = $"Contractor{id}",
                    ParentId = 0,
                    SubdivisionId = Subdivision.Telemart.Id
                };
            }
        }

        private static PagedResult<EmployeeDto> GetEmployees()
        {
            EmployeeDto CreateFunc(int id) => new EmployeeDto
            {
                Id = id,
                Name = $"User{id}",
                Active = true,
                Roles = new List<string> { Role.Admin.Name }
            };

            return new PagedResult<EmployeeDto> { Data = GenerateTestRecords(CreateFunc, 20) };
        }

        private static PagedResult<WarehouseDto> GetWarehouses()
        {
            return new PagedResult<WarehouseDto> { Data = new List<WarehouseDto>() };
        }

        private static List<DeliveryDto> GetWarehouseDeliveries()
        {
            return new List<DeliveryDto>();
        }

        private static List<WarehousePerformanceDto> GetWarehousePerformances()
        {
            return new List<WarehousePerformanceDto>();
        }

        private static PagedResult<OrganizationDto> GetOrganizations()
        {
            return new PagedResult<OrganizationDto> { Data = new List<OrganizationDto>() };
        }

        private static List<CashboxDto> GetCashboxes()
        {
            return new List<CashboxDto>();
        }

        private static PagedResult<AssemblyServiceDto> GetAssemblyServices()
        {
            return new PagedResult<AssemblyServiceDto> { Data = new List<AssemblyServiceDto>() };
        }

        private static PagedResult<AdditionalServiceProductDto> GetAdditionalServiceProducts()
        {
            return new PagedResult<AdditionalServiceProductDto> { Data = new List<AdditionalServiceProductDto>() };
        }

        private static PagedResult<AdditionalServiceDto> GetAdditionalServices()
        {
            return new PagedResult<AdditionalServiceDto> { Data = new List<AdditionalServiceDto>() };
        }

        private static PagedResult<PromoDto> GetPromos()
        {
            return new PagedResult<PromoDto> { Data = new List<PromoDto>() };
        }

        private static List<HashtagDto> GetHashtagsByPhone()
        {
            return new List<HashtagDto> {  };
        }

        private static List<OrderStateChangeReasonDto> GetOrderStateChangeReasons()
        {
            return new List<OrderStateChangeReasonDto>();
        }

        private static List<LegalEntityDto> GetLegalEntities()
        {
            return new List<LegalEntityDto>();
        }

        private static List<OrderDocumentSimpleDto> GetOrderDocuments()
        {
            return new List<OrderDocumentSimpleDto>();
        }

        private static List<OrderDocumentTypeDto> GetOrderDocumentTypes()
        {
            return new List<OrderDocumentTypeDto>();
        }

        private static string GetResult()
        {
            return "0";
        }
    }
}