using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Core;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.ViewModels;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.Views;
using Telemart.Client.Views.Accessory;
using Telemart.Client.Views.AdditionalService;
using Telemart.Client.Views.AdditionalServiceProduct;
using Telemart.Client.Views.Area;
using Telemart.Client.Views.AssembledComputerRule;
using Telemart.Client.Views.AssemblyFullRule;
using Telemart.Client.Views.AssemblyService;
using Telemart.Client.Views.AssemblyTest;
using Telemart.Client.Views.Backlog;
using Telemart.Client.Views.Bitrix;
using Telemart.Client.Views.Carry;
using Telemart.Client.Views.Cashbox;
using Telemart.Client.Views.Cities;
using Telemart.Client.Views.Comment;
using Telemart.Client.Views.CompanyStructure;
using Telemart.Client.Views.Complaint;
using Telemart.Client.Views.Content.ProductDescription;
using Telemart.Client.Views.Content.ProductEquipment;
using Telemart.Client.Views.Content.ProductFeatureGroups;
using Telemart.Client.Views.Content.ProductImages;
using Telemart.Client.Views.Content.ProductVideos;
using Telemart.Client.Views.Customer;
using Telemart.Client.Views.Directories.Category;
using Telemart.Client.Views.Directories.Contractor;
using Telemart.Client.Views.Directories.Employee;
using Telemart.Client.Views.Directories.Organization;
using Telemart.Client.Views.Directories.ProductsCatalog;
using Telemart.Client.Views.Directories.ProductsFeatures;
using Telemart.Client.Views.Directories.ProductsPrices;
using Telemart.Client.Views.Discussions;
using Telemart.Client.Views.District;
using Telemart.Client.Views.History;
using Telemart.Client.Views.LogisticsAnalitics;
using Telemart.Client.Views.LogisticsMap;
using Telemart.Client.Views.Money.Receive;
using Telemart.Client.Views.Money.Refund;
using Telemart.Client.Views.Nomenclature;
using Telemart.Client.Views.Notification;
using Telemart.Client.Views.Novaposhta;
using Telemart.Client.Views.Novaposhta.NovaposhtaBill;
using Telemart.Client.Views.Parser.Dictionary;
using Telemart.Client.Views.Parser.Monitoring;
using Telemart.Client.Views.Payment;
using Telemart.Client.Views.ProductCompatibility;
using Telemart.Client.Views.PromoCode;
using Telemart.Client.Views.Reporting;
using Telemart.Client.Views.RobotProperties;
using Telemart.Client.Views.SalesMap;
using Telemart.Client.Views.Segment;
using Telemart.Client.Views.Service.ServiceCenters;
using Telemart.Client.Views.Service.ServiceInvoices;
using Telemart.Client.Views.Service.ServiceMovements;
using Telemart.Client.Views.Service.ServiceProducts;
using Telemart.Client.Views.Service.ServiceRepairs;
using Telemart.Client.Views.Service.ServiceRequests;
using Telemart.Client.Views.Settings.Printing;
using Telemart.Client.Views.Shops;
using Telemart.Client.Views.Showcase;
using Telemart.Client.Views.Store;
using Telemart.Client.Views.Store.Call;
using Telemart.Client.Views.Store.FiscalRegistrar;
using Telemart.Client.Views.Store.ReturnInvoice;
using Telemart.Client.Views.SupplierBill;
using Telemart.Client.Views.SupplierCurrency;
using Telemart.Client.Views.Tasks;
using Telemart.Client.Views.Tools;
using Telemart.Client.Views.Tools.ChangeLegalEntity;
using Telemart.Client.Views.Tools.Tags;
using Telemart.Client.Views.Tools.UnlockDocument;
using Telemart.Client.Views.TradeIn;
using Telemart.Client.Views.TradeInSegment;
using Telemart.Client.Views.Warehouse;
using Telemart.Client.Views.Warehouse.Inventory;
using Telemart.Client.Views.Warehouse.Movement;
using Telemart.Client.Views.WorkSchedules;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.Common.Navigation
{
    public sealed class NavigationMenuBuilder : INavigationMenuBuilder
    {
        private readonly IFiscalRegistrarClientFactory _fiscalRegistrarClientFactory;
        private readonly ILogger<NavigationMenuBuilder> _logger;
        private readonly IServiceProvider _serviceProvider;

        public NavigationMenuBuilder(
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IServiceProvider serviceProvider,
            ILogger<NavigationMenuBuilder> logger)
        {
            _fiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IEnumerable<NavigationMenuGroup> BuildNavigationMenu(IReadOnlyCollection<string> roles, IReadOnlyCollection<BusinessOperation> operations)
        {
            yield return new NavigationMenuGroup("Магазин", GetStoreMenuItems().Where(x => x.Allowed(roles, operations)));
            yield return new NavigationMenuGroup("Склад", GetWarehouseMenuItems().Where(x => x.Allowed(roles, operations)));
            yield return new NavigationMenuGroup("Сервис", GetServiceMenuItems().Where(x => x.Allowed(roles, operations)));
            yield return new NavigationMenuGroup("Деньги", GetMoneyMenuItems().Where(x => x.Allowed(roles, operations)));
            yield return new NavigationMenuGroup("Справочники", GetDictionariesMenuItems().Where(x => x.Allowed(roles, operations)), false);
            yield return new NavigationMenuGroup("Контент", GetContentMenuItems().Where(x => x.Allowed(roles, operations)), false);
            yield return new NavigationMenuGroup("Правила и зависимости", GetRulesAndDependenciesItems().Where(x => x.Allowed(roles, operations)), false);
            yield return new NavigationMenuGroup("Идеи и задачи", GetTasksMenuItems().Where(x => x.Allowed(roles, operations)), false);
            yield return new NavigationMenuGroup("Новая Почта", GetNovaposhtaMenuItems().Where(x => x.Allowed(roles, operations)), false);
            yield return new NavigationMenuGroup("Инструменты", GetToolsMenuItems().Where(x => x.Allowed(roles, operations)), false);
        }

        private static IEnumerable<NavigationMenuItem> GetWarehouseMenuItems()
        {
            yield return new NavigationMenuItem(
                "Накладные",
                "cheque",
                typeof(StoreInvoicesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.InvoicesModuleAccess, BusinessOperation.SupplierBillCreateByInvoice },
                disallowedByOperation: new[] { BusinessOperation.Intern });

            yield return new NavigationMenuItem(
               "Возвраты",
               "arrow_redo_16x16",
               typeof(StoreReturnInvoicesView),
               Array.Empty<Role>(),
               new[] { BusinessOperation.ReturnInvoicesModuleAccess, BusinessOperation.SupplierBillCreateByInvoice });

            yield return new NavigationMenuItem(
                "Упаковка",
                "box_open",
                typeof(StoreOrdersPackView),
                new[] { Role.Admin, Role.TechSupport, Role.Warehouse, Role.Packager, Role.Seller },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Сборка",
                "computer",
                typeof(AssemblyServicesView),
                new[] { Role.Admin, Role.TechSupport, Role.Assembler },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Оказание услуг",
                "server_lightning",
                typeof(AdditionalServiceProductsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.AdditionalServiceProductModuleAccess });

            yield return new NavigationMenuItem(
                "Перемещения",
                "house_go",
                typeof(MovementsView),
                Array.Empty<Role>(),
                new[] {BusinessOperation.MovementModuleAccess });

            yield return new NavigationMenuItem(
                "Инвентаризация",
                "house_table",
                typeof(WarehouseInventoriesView),
                new[] { Role.Admin, Role.TechSupport, Role.Warehouse, Role.Seller, Role.OutsourceSeller },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Маршруты",
                "house_go",
                typeof(WarehousesConnectionsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.WarehouseGetConnections });

            yield return new NavigationMenuItem(
                "Планировщик",
                "date_next",
                typeof(LogisticsAnaliticsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.LogisticsAnaliticsModuleAccess });
        }

        private static IEnumerable<NavigationMenuItem> GetServiceMenuItems()
        {
            yield return new NavigationMenuItem(
                "Trade-In",
                "ruby",
                typeof(TradeInsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.TradeInsModuleAccess });

            yield return new NavigationMenuItem(
                "Ремонт",
                "wrench_orange",
                typeof(ServiceRepairsView),
                new[] { Role.Admin, Role.TechSupport, Role.ServiceManager },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Серв. заявки",
                "toolbox",
                typeof(ServiceRequestsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ServiceRequestModuleAccess });

            yield return new NavigationMenuItem(
                "Серв. накладные",
                "cheque_wrench",
                typeof(ServiceInvoicesView),
                new[] { Role.Admin, Role.TechSupport, Role.ServiceManager, Role.Logist },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Серв. перемещения",
                "house_go",
                typeof(ServiceMovementsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ServiceMovementAccess });

            yield return new NavigationMenuItem(
                "Серв. товары",
                "house_bug",
                typeof(ServiceProductsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ServiceProductsModuleAccess });

            yield return new NavigationMenuItem(
                "Серв. центры",
                "house_wrench_16x16",
                typeof(ServiceCentersView),
                new[] { Role.Admin, Role.TechSupport, Role.ServiceManager },
                Array.Empty<BusinessOperation>());
        }

        private static IEnumerable<NavigationMenuItem> GetMoneyMenuItems()
        {
            yield return new NavigationMenuItem(
                "Внесение ДС",
                "coins_add",
                typeof(ProcessReceivedMoneyView),
                new[] { Role.Admin },
                new[] { BusinessOperation.ReceiveMoney },
                navigationMode: NavigationMode.SizeableDialog);

            yield return new NavigationMenuItem(
                "Возврат ДС",
                "coins_delete",
                typeof(RefundsView),
                new[] { Role.Admin, Role.TechSupport, Role.Manager, Role.Operator, Role.ServiceManager, Role.Seller, Role.Accountant },
                new[] { BusinessOperation.RefundMoney });

            yield return new NavigationMenuItem(
                "Выписка",
                "money_bag",
                typeof(BankPaymentsView),
                new[] { Role.Admin, Role.Accountant },
                Array.Empty<BusinessOperation>());
        }

        private static IEnumerable<NavigationMenuItem> GetContentMenuItems()
        {
            yield return new NavigationMenuItem(
                "Видео",
                "video",
                typeof(ProductVideosView),
                new[] { Role.Admin, Role.TechSupport, Role.Content },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                ModuleNameConstants.CommentsModule,
                "comment",
                typeof(CommentsView),
                new[] { Role.Admin, Role.TechSupport, Role.Content, Role.Product },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Инфо о товаре",
                "text_list_bullets",
                typeof(ProductEquipmentsView),
                new[] { Role.Admin, Role.TechSupport, Role.Content },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Наборы хар-ик",
                "table_gear",
                typeof(ProductFeaturesCatalogView),
                new[] { Role.Admin, Role.TechSupport, Role.Content },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Фото",
                "images_16x16",
                typeof(ProductImagesView),
                new[] { Role.Admin, Role.TechSupport, Role.Content },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Описание",
                "text",
                typeof(ProductDescriptionsView),
                new[] { Role.Admin, Role.TechSupport, Role.Content },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Характеристики",
                "gear",
                typeof(ProductFeaturesView),
                new[] { Role.Admin, Role.TechSupport, Role.Content, Role.Product },
                Array.Empty<BusinessOperation>(),
                5);
        }

        private static IEnumerable<NavigationMenuItem> GetRulesAndDependenciesItems()
        {
            yield return new NavigationMenuItem(
                "Аксессуары",
                "module",
                typeof(AccessoriesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.AccessoryModuleAccess });

            yield return new NavigationMenuItem(
                "Полноценность сборки",
                "computer",
                typeof(AssemblyFullRulesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.AssemblyFullRuleModuleAccess });

            yield return new NavigationMenuItem(
                "Совместимость",
                "pci",
                typeof(ProductCompatibilitiesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ProductCompatibilityModuleAccess });

            yield return new NavigationMenuItem(
                "Конфигурации ПК",
                "server",
                typeof(AssembledComputerRulesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.AssembledComputerRuleModuleAccess });

            yield return new NavigationMenuItem(
                "Тесты",
                "check_box_list",
                typeof(AssemblyTestsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.AssemblyTestModuleAccess });

            yield return new NavigationMenuItem(
                "Услуги",
                "server_configuration",
                typeof(AdditionalServicesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.AdditionalServicesModuleAccess });
        }

        private static IEnumerable<NavigationMenuItem> GetTasksMenuItems()
        {
            yield return new NavigationMenuItem(
                "Backlog",
                "accordion",
                typeof(BacklogTasksView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.BacklogModuleAccess });

            yield return new NavigationMenuItem(
                "Задачи",
                "sheduled_task",
                typeof(TasksView),
                null,
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Задачи B24",
                "bitrix24",
                typeof(BitrixTasksView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.BitrixTasksAccess });

            yield return new NavigationMenuItem(
                "Обсуждения",
                "user_comment_16x16",
                typeof(DiscussionsView),
                null,
                Array.Empty<BusinessOperation>());
        }

        private IEnumerable<NavigationMenuItem> GetStoreMenuItems()
        {
            yield return new NavigationMenuItem(
                "Заказы",
                "cart",
                typeof(StoreOrdersView),
                new[] { Role.Admin, Role.TechSupport, Role.Manager, Role.Warehouse, Role.Product, Role.Logist, Role.Seller, Role.Operator, Role.ServiceManager, Role.Packager, Role.Accountant, Role.OutsourceSeller },
                Array.Empty<BusinessOperation>(),
                4);

            yield return new NavigationMenuItem(
                "Закупки",
                "lorry",
                typeof(StorePurchasesView),
                new[] { Role.Admin, Role.Product },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Звонки",
                "headphone",
                typeof(StoreCallsView),
                new[] { Role.Admin, Role.TechSupport, Role.Operator, Role.Manager },
                Array.Empty<BusinessOperation>(),
                2);

            yield return new NavigationMenuItem(
                "Клиенты",
                "users_4",
                typeof(CustomersView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.CustomerModuleAccess });

            yield return new NavigationMenuItem(
                "Прайс-лист",
                "table_excel",
                typeof(DownloadPriceListView),
                new[] { Role.Admin, Role.Manager },
                Array.Empty<BusinessOperation>(),
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "Счета поставщиков",
                "receipt_invoice",
                typeof(SupplierBillsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.SupplierBillModuleAccess });

            yield return new NavigationMenuItem(
                "Ценники",
                "three_tags",
                typeof(TagsView),
                new[] { Role.Admin, Role.TechSupport, Role.Seller, Role.OutsourceSeller },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Цены",
                "coins",
                typeof(ProductPricesView),
                new[] { Role.Admin, Role.Product },
                Array.Empty<BusinessOperation>(),
                10);

            yield return new NavigationMenuItem(
                "Робот",
                "android_16x16",
                typeof(RobotPropertiesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.RobotPropertiesModuleAccess });

            yield return new NavigationMenuItem(
                "Курсы поставщиков",
                "conversion_of_currency",
                typeof(SupplierCurrenciesView),
                new[] { Role.Admin },
                new[] { BusinessOperation.SupplierCurrencyRateModule });

            yield return new NavigationMenuItem(
                "Витрина",
                "shop",
                typeof(AutoShowcasesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ShowcaseModuleAccess });

            yield return new NavigationMenuItem(
                "История витрин",
                "shop",
                typeof(ShowcasesView),
                new[] { Role.Admin },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Акции",
                "tag_blue",
                typeof(PromoCodesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.PromoCodeModuleAccess });

            yield return new NavigationMenuItem(
                "Журнал жалоб",
                "thumb_down",
                typeof(ComplaintsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ComplaintsModuleAccess });

            Result result = AsyncHelper.RunSync(() => _fiscalRegistrarClientFactory.CreateAsync(CancellationToken.None));

            if (result.IsSuccess)
            {
                yield return new NavigationMenuItem(
                    "РРО",
                    "cash_register_left",
                    typeof(StoreFiscalRegistrarView),
                    new[] { Role.Admin, Role.Seller },
                    Array.Empty<BusinessOperation>(),
                    navigationMode: NavigationMode.NotModalSizeableDialog);
            }
            else
            {
                _logger.LogWarning("Errors by creating fiscal registrar. Errors: {Errors}", string.Join(", ", result.ErrorObj.GetMessages()));
            }
        }

        private IEnumerable<NavigationMenuItem> GetDictionariesMenuItems()
        {
            yield return new NavigationMenuItem(
                "Города",
                "building",
                typeof(CitiesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.CityModuleAccess });

            yield return new NavigationMenuItem(
                "Районы",
                "district",
                typeof(DistrictsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.DistrictModuleAccess });

            yield return new NavigationMenuItem(
                "Области",
                "area",
                typeof(AreasView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.AreaModuleAccess });

            yield return new NavigationMenuItem(
                "Категории",
                "folder_wrench",
                typeof(DirectoryCategoriesView),
                new[] { Role.Admin, Role.Manager, Role.Warehouse, Role.Product, Role.Logist, Role.Operator, Role.Marketer },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Сегменты",
                "segment",
                typeof(SegmentsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.SegmentModuleAccess });

            yield return new NavigationMenuItem(
                "Сегменты Trade-In",
                "segment_tradein",
                typeof(TradeInSegmentsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.TradeInSegmentModuleAccess });

            yield return new NavigationMenuItem(
                "Кассы",
                "cash_stack",
                typeof(CashboxesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.CashboxModuleAccess });

            yield return new NavigationMenuItem(
                "Контрагенты",
                "user_suit",
                typeof(DirectoryContractorsView),
                new[] { Role.Admin, Role.Manager, Role.Product, Role.Logist, Role.ServiceManager, Role.Accountant },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Номенклатура",
                "database_table",
                typeof(ProductsCatalogView),
                new[] { Role.Admin, Role.TechSupport, Role.Product, Role.Content, Role.Marketer },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Организации",
                "chart_organisation",
                typeof(OrganizationsView),
                new[] { Role.Admin, Role.TechSupport, Role.Accountant, Role.ServiceManager },
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Склады",
                "house",
                typeof(WarehousesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.WarehouseModuleAccess });

            yield return new NavigationMenuItem(
                "Локации",
                "Location",
                typeof(LocationsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ShopModuleAccess });

            yield return new NavigationMenuItem(
                "Словарь товаров",
                "book_key",
                typeof(ParserDictionaryView),
                new[] { Role.Admin, Role.TechSupport, Role.Product, Role.Content },
                Array.Empty<BusinessOperation>(),
                viewModel: _serviceProvider.GetService<ProductParserDictionaryViewModel>());

            yield return new NavigationMenuItem(
                "Словарь характеристик",
                "shape_square_key",
                typeof(ParserDictionaryView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.DictionariesModulesAccess },
                viewModel: _serviceProvider.GetService<FeatureParserDictionaryViewModel>());

            yield return new NavigationMenuItem(
                "Словарь значений хар-ик",
                "table_key",
                typeof(ParserDictionaryView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.DictionariesModulesAccess },
                viewModel: _serviceProvider.GetService<FeatureValueParserDictionaryViewModel>());

            yield return new NavigationMenuItem(
                "Орг. структура",
                "org_structure_users",
                typeof(CompanyStructureView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.CompanyStructureModule });

            yield return new NavigationMenuItem(
                "Сотрудники",
                "report_user",
                typeof(DirectoryEmployeesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.EmployeesModulesAccess });

            yield return new NavigationMenuItem(
                "Рабочие графики",
                "calendar",
                typeof(WorkSchedulesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.WorkSchedulesModuleAccess });

            yield return new NavigationMenuItem(
                "Способы доставки",
                "steering_wheel_racing",
                typeof(CarriesView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.CarryUpdate });

            yield return new NavigationMenuItem(
                "Способы оплаты",
                "table_money",
                typeof(PaymentsView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.PaymentTypeModuleAccess });

            NomenclatureViewOptions nomenclatureViewOptions = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false,
                fullScreenMode: true);

            yield return new NavigationMenuItem(
                "Товары",
                "box_front_open",
                typeof(NomenclatureView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ProductsModuleAccess },
                parameter: nomenclatureViewOptions);
        }

        private IEnumerable<NavigationMenuItem> GetNovaposhtaMenuItems()
        {
            yield return new NavigationMenuItem(
                "Внести ТТН",
                "box_down",
                typeof(AddNovaposhtaTtnView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.NpDocumentRegister },
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "Создать ТТН",
                "add_package",
                typeof(CreateNovaposhtaTtnView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.NpDocumentCreate },
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "Счета НП",
                "novaposhta",
                typeof(NovaposhtaBillView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.NpBillUpdate },
                navigationMode: NavigationMode.Dialog);
        }

        private IEnumerable<NavigationMenuItem> GetToolsMenuItems()
        {
            yield return new NavigationMenuItem(
                "Вики",
                "bookshelf",
                typeof(WatsNewView),
                null,
                Array.Empty<BusinessOperation>(),
                parameter: new TelewikiParameter(null));

            yield return new NavigationMenuItem(
                "Уведомления",
                "comments_16x16",
                typeof(NotificationsView),
                null,
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Мониторинг (Old)",
                "monitoring",
                typeof(ParsersView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.MonitoringModuleAccess });

            yield return new NavigationMenuItem(
                "Мониторинг",
                "monitoring",
                typeof(ParserCronicleView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.MonitoringModuleAccess });

            yield return new NavigationMenuItem(
                "Настройки",
                "gear",
                typeof(PrintingSettingsView),
                null,
                Array.Empty<BusinessOperation>(),
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "Отчеты",
                "report",
                typeof(ReportSelectionView),
                null,
                Array.Empty<BusinessOperation>(),
                navigationMode: NavigationMode.SizeableDialog);

            yield return new NavigationMenuItem(
                "BI",
                "metabase",
                typeof(MetabaseView),
                null,
                Array.Empty<BusinessOperation>());

            yield return new NavigationMenuItem(
                "Карта продаж",
                "google_map_satellite",
                typeof(SalesMapView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.SalesMapModuleAccess });

            yield return new NavigationMenuItem(
                "Карта логистики",
                "map_logo",
                typeof(LogisticsMapView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.LogisticsMapModuleAccess });

            yield return new NavigationMenuItem(
                "Разблокировка",
                "lock_break",
                typeof(UnlockDocumentView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.UnlockEntity },
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "Изменение юр. лица",
                "account_functions",
                typeof(ChangeLegalEntityView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.ChangeLegalEntity },
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "Изменение статуса",
                "lightbulb_delete",
                typeof(ChangeStateView),
                new[] { Role.Admin, Role.TechSupport },
                Array.Empty<BusinessOperation>(),
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "Провести",
                "database_go",
                typeof(RecoverOrdersView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.RecoverOrders },
                navigationMode: NavigationMode.Dialog);

            yield return new NavigationMenuItem(
                "История SN",
                "barcode",
                typeof(SerialNumberHistoryView),
                Array.Empty<Role>(),
                new[] { BusinessOperation.SerialNumberViewHistory },
                navigationMode: NavigationMode.Dialog);
        }
    }
}