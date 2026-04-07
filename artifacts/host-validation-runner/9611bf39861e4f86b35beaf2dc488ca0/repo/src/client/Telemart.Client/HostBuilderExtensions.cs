using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Quartz;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Telemart.Client.AutoMappings.Profiles;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Business.Parser;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Business.Schedule;
using Telemart.Client.Business.ServiceRequest;
using Telemart.Client.Business.Tag;
using Telemart.Client.Cache;
using Telemart.Client.Common;
using Telemart.Client.Common.Calculators;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.HubFactory;
using Telemart.Client.Common.Layouts;
using Telemart.Client.Common.Navigation;
using Telemart.Client.Common.PosTerminal;
using Telemart.Client.Common.ProductDescription;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Common.Settings.ModuleAnalytics;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Core;
using Telemart.Client.Core.IO;
using Telemart.Client.Core.Security;
using Telemart.Client.Core.Serialization;
using Telemart.Client.Core.Serialization.Json;
using Telemart.Client.Core.Update;
using Telemart.Client.Data;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Diagnostics;
using Telemart.Client.Data.HubClient.Base;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Stores;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions.ContainerExtensions;
using Telemart.Client.Factories.Complaint;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.Helpers;
using Telemart.Client.Jobs;
using Telemart.Client.Oktell;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.PosTerminal.PrivatBank;
using Telemart.Client.Reports.ReportBuilders.Base;
using Telemart.Client.SingleInstance;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.NovaposhtaBill;
using Telemart.Client.TransferObjects.ParserSettings;
using Telemart.Client.Validators;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Content.FeatureImages;
using Telemart.Client.ViewModels.Content.ProductDescription;
using Telemart.Client.ViewModels.Content.ProductEquipments;
using Telemart.Client.ViewModels.Content.ProductImages;
using Telemart.Client.ViewModels.Content.ProductVideos;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Directories.ProductsCatalog;
using Telemart.Client.ViewModels.Directories.ProductsPrices;
using Telemart.Client.ViewModels.Money.Receive;
using Telemart.Client.ViewModels.Money.Receive.BankPayment;
using Telemart.Client.ViewModels.Novaposhta.NovaposhtaBill;
using Telemart.Client.ViewModels.Service.ServiceRequests;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.WebClient;
using Telemart.Client.WebClient.Jobs;
using Telemart.Client.WebClient.Prices;
using Telemart.PriceCalculation;

namespace Telemart.Client
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureTelemartHost(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(ConfigureTelemartServices);

            return hostBuilder;
        }

        private static void ConfigureTelemartServices(HostBuilderContext context, IServiceCollection serviceCollection)
        {
            IConfiguration configurationManager = context.Configuration;

            serviceCollection.AddOptions();

            serviceCollection.AddOptions<NovaPayOptions>(configurationManager.GetSection("NovaPay"));
            serviceCollection.AddOptions<UpdateManagerOptions>(configurationManager.GetSection("UpdateManager"));
            serviceCollection.AddOptions<FiscalOptions>(configurationManager.GetSection("FiscalSettings"));
            serviceCollection.AddOptions<EmployeeOptions>(configurationManager.GetSection("EmployeeSettings"));
            serviceCollection.AddOptions<AppOptions>(configurationManager.GetSection("AppSettings"));
            serviceCollection.AddOptions<SchedulerOptions>(configurationManager.GetSection("Scheduler"));
            serviceCollection.AddOptions<PricesGrpcServiceOptions>(configurationManager.GetSection("GrpcService:Prices"));
            serviceCollection.AddOptions<ParserServiceOptions>(configurationManager.GetSection("GrpcService:Parser"));
            serviceCollection.AddOptions<TelewikiOptions>(configurationManager.GetSection("Telewiki"));
            serviceCollection.AddOptions<MetabaseOptions>(configurationManager.GetSection("Metabase"));
            serviceCollection.AddOptions<CronicleOptions>(configurationManager.GetSection("ParserCronicle"));
            serviceCollection.AddOptions<NetworkDiagnoserOptions>(configurationManager.GetSection("NetworkDiagnoser"));
            serviceCollection.AddOptions<CallTrackOptions>(configurationManager.GetSection("CallTrack"));
            serviceCollection.AddOptions<FileUploadOptions>(configurationManager.GetSection("FileUpload"));
            serviceCollection.AddOptions<ChunkOptions>(configurationManager.GetSection("Chunk"));
            serviceCollection.AddOptions<PrintRroOptions>(configurationManager.GetSection("PrintRro"));
            serviceCollection.AddOptions<TerminalOptions>(configurationManager.GetSection("Termіnal"));
            serviceCollection.AddOptions<TelegramBotOptions>(configurationManager.GetSection("TelegramBot"));

            serviceCollection.AddService<IdentityServiceOptions>(configurationManager.GetSection("RestService:Identity"), Services.Identity, false);
            serviceCollection.AddService<MainServiceOptions>(configurationManager.GetSection("RestService:Main"), Services.Main);
            serviceCollection.AddService<CatalogServiceOptions>(configurationManager.GetSection("RestService:Catalog"), Services.Catalog);
            serviceCollection.AddService<ReportServiceOptions>(configurationManager.GetSection("RestService:Report"), Services.Report);
            serviceCollection.AddService<TelegramServiceOptions>(configurationManager.GetSection("RestService:Telegram"), Services.Telegram);
            serviceCollection.AddService<PricesServiceOptions>(configurationManager.GetSection("RestService:Prices"), Services.Prices);
            serviceCollection.AddService<FiscalServiceOptions>(configurationManager.GetSection("RestService:Fiscal"), Services.Fiscal);
            serviceCollection.AddService<CallServiceOptions>(configurationManager.GetSection("RestService:Call"), Services.Call);

            string logsFolderPath = Path.Combine(ApplicationFolders.LocalApplicationData, "logs");

            const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [{ThreadId}] [{SourceContext}] {Properties:j}{Message}{NewLine}{Exception}";

            Logger logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configurationManager)
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Logger(lc => lc.Filter.ByIncludingOnly(Matching.FromSource<OrderConfirmViewModel>())
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(logsFolderPath, "order-confirmation-.log"), outputTemplate: OutputTemplate, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day))
                .WriteTo.File(
                    Path.Combine(logsFolderPath, "log-.log"),
                    outputTemplate: OutputTemplate,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    fileSizeLimitBytes: 103809024,
                    shared: true,
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            serviceCollection.AddLogging(x => x.AddSerilog(logger));

            serviceCollection.AddQuartz(x => x.UseMicrosoftDependencyInjectionJobFactory());

            serviceCollection.AddAutoMapper(typeof(OrderMappingProfile).Assembly);

            serviceCollection.AddMediatR(x => x.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

            serviceCollection.AddSingleton<IParserClient, ParserClient>();

            serviceCollection.AddMemoryCache();

            serviceCollection.RegisterReportPrinters(typeof(ReportPrinterBase<>).Assembly);
            serviceCollection.RegisterHubs(typeof(HubClientBase<>).Assembly);
            serviceCollection.RegisterViewModels(typeof(OrderViewModel).Assembly);
            serviceCollection.RegisterSyncServices();
            serviceCollection.RegisterCarryProviders();

            serviceCollection.AddSingleton<IUpdateManager, UpdateManager>();
            serviceCollection.AddSingleton<INavigationMenuBuilder, NavigationMenuBuilder>();

            serviceCollection.AddSingleton<ITagPrinterFactory, TagPrinterFactory>();

            serviceCollection.AddSingleton<OrderComplaintCreator>();
            serviceCollection.AddSingleton<ServiceRequestComplaintCreator>();
            serviceCollection.AddSingleton<TradeInComplaintCreator>();
            serviceCollection.AddSingleton<EmptyComplaintCreator>();
            serviceCollection.AddSingleton<IReportPrintHelper, ReportPrintHelper>();
            serviceCollection.AddSingleton<IModuleLayoutService, ModuleLayoutService>();
            serviceCollection.AddSingleton<SingleInstanceAppProcessor>();
            serviceCollection.AddSingleton(typeof(IFilterModuleLayoutService<>), typeof(FilterModuleLayoutService<>));
            serviceCollection.AddSingleton<IPriceConverterFactory, PriceConverterFactory>();
            serviceCollection.AddSingleton<IAuthenticationManager, AuthenticationManager>();
            serviceCollection.AddTransient<IRestClientGateway, RestClientGateway>();
            serviceCollection.AddSingleton<IMessageFacadeService, MessageFacadeServive>();
            serviceCollection.AddSingleton<IPriceConverterFactory, PriceConverterFactory>();
            serviceCollection.AddSingleton<INetworkDiagnoser, NetworkDiagnoser>();
            serviceCollection.AddSingleton<ITelemartClientLogger, TelemartClientLogger>();
            serviceCollection.AddSingleton<IPricesClient, PricesWebClient>();
            serviceCollection.AddSingleton<IWebClient, TelemartWebClient>();
            serviceCollection.AddSingleton<UpdateAuthenticatedEmployeeJob>();
            serviceCollection.AddSingleton<IDictionaries, Dictionaries.Dictionaries>();
            serviceCollection.AddSingleton<IOrderRules, OrderRules>();
            serviceCollection.AddSingleton<IOrderDeliveryCalculator, OrderDeliveryCalculator>();
            serviceCollection.AddSingleton<IOrderReportBuilder, OrderReportBuilder>();
            serviceCollection.AddSingleton<ILockableOperationProcessorFactory, LockableOperationProcessorFactory>();
            serviceCollection.AddSingleton(typeof(LockableOperationProcessor<>));
            serviceCollection.AddSingleton<IDispatcherService, DispatcherService>();
            serviceCollection.AddSingleton<IPosTerminalFactory, PosTerminalFactory>();
            serviceCollection.AddSingleton<IServiceRequestPrinter, ServiceRequestPrinter>();
            serviceCollection.AddSingleton(_ => Messenger.Default);
            serviceCollection.AddSingleton(_ => ServiceCollectionExtensions.CreateNotificationService());
            serviceCollection.AddSingleton(_ => ServiceCollectionExtensions.CreateNewCommentNotificationService());
            serviceCollection.AddSingleton(_ => ServiceCollectionExtensions.CreateEntityNotificationService());
            serviceCollection.AddSingleton<IPrivatBankPosTerminalClient, PrivatPosTerminalClient>();
            serviceCollection.AddSingleton<IngenicoPosTerminalClient>();
            serviceCollection.AddSingleton<IngenicoUkrsibbankPosTerminalClient>();
            serviceCollection.AddSingleton<IErrorHandler, ResultErrorHandler>();
            serviceCollection.AddSingleton<IIdGenerator, IdGenerator>();
            serviceCollection.AddSingleton<ISerializerBuilder, JsonSerializerBuilder>();
            serviceCollection.AddSingleton<IPrintingSettingsStore, PrintingSettingsStore>();
            serviceCollection.AddSingleton<IEquipmentSettingsStore, EquipmentSettingsStore>();
            serviceCollection.AddSingleton<IModuleAnalyticsSettingsStore, ModuleAnalyticsSettingsStore>();
            serviceCollection.AddSingleton<IOrderGiveHelper, OrderGiveHelper>();
            serviceCollection.AddSingleton<IRroPrintHelper, RroPrintHelper>();
            serviceCollection.AddSingleton<ICallHelper, CallHelper>();
            serviceCollection.AddSingleton<ICryptoManager, CryptoManager>();
            serviceCollection.AddSingleton<IComparsionManager, ComparsionManager>();
            serviceCollection.AddSingleton<ICategoryOptionsInitializer, CategoryOptionsInitializer>();
            serviceCollection.AddSingleton<IPasswordGenerator, PasswordGenerator>();
            serviceCollection.AddSingleton<ITimeProvider, CurrentTimeProvider>();
            serviceCollection.AddSingleton<IBulkAddTextProcessor, BulkAddTextProcessor>();
            serviceCollection.AddSingleton<IFileSystem, FileSystem>();
            serviceCollection.AddSingleton<IProductImageFileValidator, ProductImageFileValidator>();
            serviceCollection.AddSingleton<IProductImagesDirectoryProcessor, ProductImagesDirectoryProcessor>();
            serviceCollection.AddSingleton<IProductDescriptionDirectoryProcessor, ProductDescriptionDirectoryProcessor>();
            serviceCollection.AddSingleton<IRobotCalculator, RobotCalculator>();
            serviceCollection.AddSingleton<IExcelImportEngine<NovaposhtaBillTtnDto>, NovaposhtaBillExcellImportEngine>();
            serviceCollection.AddSingleton<IExcelImportEngine<ProductVideoViewItem>, ProductVideoExcelImportEngine>();
            serviceCollection.AddSingleton<IExcelImportSettingsEngine<ProductEquipmentViewItem, ProductInfoType>, ProductEquipmentsExcelImportEngine>();
            serviceCollection.AddSingleton<IExcelImportEngine<BulkCreateBankPaymentViewItem>, BulkCreateBankPaymentExcelImportEngine>();
            serviceCollection.AddSingleton<IExcelImportEngine<ReceiveViewItem>, ReceiveMoneyExcelImportEngine>();

            serviceCollection.AddSingleton<IExcelImportSettingsEngine<ProductSaveDto, ParserSettingsDto>, ExcelPricesImportEngine>();
            serviceCollection.AddSingleton<UkrposhtaReceiveMoneyExcelImportEngine>();
            serviceCollection.AddSingleton<MeestReceiveMoneyExcelImportEngine>();
            serviceCollection.AddSingleton<IExcelImportSettingsEngineAsync<ProductCatalogImportItem, ProductsCatalogExcelImportSettings>, ProductsCatalogExcelImportEngine>();
            serviceCollection.AddSingleton<IProductDescriptionBuilder, ProductDescriptionBuilder>();
            serviceCollection.AddSingleton<IFileDownloader, FileDownloader>();
            serviceCollection.AddSingleton<OktellClient>();
            serviceCollection.AddSingleton<AsteriskClient>();
            serviceCollection.AddSingleton(ServiceCollectionExtensions.GetCallClient);
            serviceCollection.AddSingleton<IResponseHandler, ResponseHandler>();
            serviceCollection.AddSingleton<IFiscalRegistrarClientFactory, FiscalRegistrarClientFactory>();
            serviceCollection.AddSingleton<DocumentCommands>();
            serviceCollection.AddSingleton<CompensationHelper>();
            serviceCollection.AddTransient<ProgrammicalFiscalRegistrarClient>();
            serviceCollection.AddTransient<FiscalRegistrarClient>();
            serviceCollection.AddSingleton<IViewModelResolver, ViewModelResolver>();
            serviceCollection.AddSingleton<IBarcodeReportFactory, BarcodeReportFactory>();
            serviceCollection.AddSingleton<IHubClientFactory, HubClientFactory>();
            serviceCollection.AddSingleton<IServiceRequestReportBuilder, ServiceRequestReportBuilder>();
            serviceCollection.AddSingleton<IInsuranceCalculator, InsuranceCalculator>();
            serviceCollection.AddSingleton<ICallStore, CallStore>();

            serviceCollection.AddSingleton<IPosSettingsValidator, PosSettingsValidator>();
            serviceCollection.AddSingleton<IFiscalSettingsValidator, FiscalSettingsValidator>();
            serviceCollection.AddSingleton<IParserProductPriceValidator, ParserProductPriceValidator>();

            serviceCollection.AddSingleton<IFiscalRegistrarSettingsChecker, FiscalRegistrarSettingsChecker>();
            serviceCollection.AddSingleton<IEquipmentSettingsWorker, EquipmentSettingsWorker>();
            serviceCollection.AddSingleton<IPosSettingsChecker, PosSettingsChecker>();
            serviceCollection.AddSingleton<OrderViewProvider>();
            serviceCollection.AddSingleton<IFeatureIconValidator, FeatureIconValidator>();
            serviceCollection.AddSingleton<IWikiHelper, WikiHelper>();
            serviceCollection.AddSingleton<ILiteDbConnectionFactory, LiteDbConnectionFactory>();
            serviceCollection.AddSingleton<ICache, LiteDbCache>();

            serviceCollection.AddHostedService<SyncCacheJob>();
            serviceCollection.AddSingleton(x => x.GetRequiredService<IEnumerable<IHostedService>>().FirstOrDefault(z => z is SyncCacheJob) as SyncCacheJob);

            // disable HttpClientFactory logging
            serviceCollection.RemoveAll<IHttpMessageHandlerBuilderFilter>();
        }
    }
}