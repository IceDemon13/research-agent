using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DevExpress.Diagram.Core;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Diagram;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.LogisticsMap;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Route;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Warehouse;
using ConnectorType = DevExpress.Diagram.Core.ConnectorType;

namespace Telemart.Client.ViewModels.LogisticsMap
{
    public class LogisticsMapViewModel : TelemartViewModelBase
    {
        private const double SupplierWarehouseXPosition = 30;
        private const int DefaultWidthCoefficient = 2;

        private readonly Brush _supplierWarehouseBackground;
        private readonly IMessenger _messenger;
        private readonly BrushConverter _converter;

        private double _mainKyivWarehouseYPosition;

        public LogisticsMapViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger;
            _converter = new BrushConverter();
            _supplierWarehouseBackground = Brushes.Gray;
            _mainKyivWarehouseYPosition = 0;

            ResetFilterCommand = new DelegateCommand(ResetFilter);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            DiagramDoubleClickCommand = new DelegateCommand<DiagramItem>(DiagramDoubleClick);
            DiagramSelectionChangedCommand = new DelegateCommand<DiagramItem>(DiagramSelectionChanged);
            OpenItemCommand = new DelegateCommand<DiagramItem>(DiagramDoubleClick, _ => SelectedDiagramItem != null);
            SelectPanToolCommand = new DelegateCommand(SelectPanTool, () => AllowSelectPanTool);
            SelectPointerToolCommand = new DelegateCommand(SelectPointerTool, () => AllowSelectPointerTool);

            WithoutLogistics = false;
            OnlyMainSupplierRoutes = true;

            WidthCoefficients = new List<int>()
            {
                1,
                DefaultWidthCoefficient,
                3,
                4,
                5,
                7,
                10,
                15,
                20
            };

            SelectedWidthCoefficient = DefaultWidthCoefficient;
        }

        public EventHandler<DiagramShape> OnDiagramShapeAdded;

        public EventHandler<DiagramConnector> OnDiagramConnectorAdded;

        public EventHandler OnDiagramCleared;

        public EventHandler PointerToolSelected;

        public EventHandler PanToolSelected;

        public IDelegateCommand DiagramDoubleClickCommand { get; }

        public IDelegateCommand DiagramSelectionChangedCommand { get; }

        public IDelegateCommand OpenItemCommand { get; }

        public IDelegateCommand SelectPanToolCommand { get; }

        public IDelegateCommand SelectPointerToolCommand { get; }

        public IDelegateCommand ResetFilterCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public ObservableCollection<WarehouseDto> AllWarehouses
        {
            get { return GetProperty(() => AllWarehouses); }
            set { SetProperty(() => AllWarehouses, value); }
        }

        public ObservableCollection<ComboBoxItem> AllWarehouseTypeIds
        {
            get { return GetProperty(() => AllWarehouseTypeIds); }
            set { SetProperty(() => AllWarehouseTypeIds, value); }
        }

        public ObservableCollection<ComboBoxItem> AllSuppliers
        {
            get { return GetProperty(() => AllSuppliers); }
            set { SetProperty(() => AllSuppliers, value); }
        }

        public List<int> WidthCoefficients
        {
            get { return GetProperty(() => WidthCoefficients); }
            set { SetProperty(() => WidthCoefficients, value); }
        }

        public int SelectedWidthCoefficient
        {
            get { return GetProperty(() => SelectedWidthCoefficient); }
            set { SetProperty(() => SelectedWidthCoefficient, value); }
        }

        public List<object> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        public List<object> SelectedSuppliers
        {
            get { return GetProperty(() => SelectedSuppliers); }
            set { SetProperty(() => SelectedSuppliers, value); }
        }

        public List<object> SelectedWarehouseTypeIds
        {
            get { return GetProperty(() => SelectedWarehouseTypeIds); }
            set { SetProperty(() => SelectedWarehouseTypeIds, value, OnSelectedWarehouseTypesChanged); }
        }

        public bool WithoutLogistics
        {
            get { return GetProperty(() => WithoutLogistics); }
            set { SetProperty(() => WithoutLogistics, value, OnWithoutLogisticsChanged); }
        }

        public bool OnlyMainSupplierRoutes
        {
            get { return GetProperty(() => OnlyMainSupplierRoutes); }
            set { SetProperty(() => OnlyMainSupplierRoutes, value, OnOnlyMainSupplierRoutesChanged); }
        }

        public DiagramItem SelectedDiagramItem
        {
            get { return GetProperty(() => SelectedDiagramItem); }
            set { SetProperty(() => SelectedDiagramItem, value); }
        }

        public string SelectedDiagramItemText
        {
            get { return GetProperty(() => SelectedDiagramItemText); }
            set { SetProperty(() => SelectedDiagramItemText, value); }
        }

        public bool AllowSelectPanTool
        {
            get { return GetProperty(() => AllowSelectPanTool); }
            set { SetProperty(() => AllowSelectPanTool, value); }
        }

        public bool AllowSelectPointerTool
        {
            get { return GetProperty(() => AllowSelectPointerTool); }
            set { SetProperty(() => AllowSelectPointerTool, value); }
        }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        protected override async Task HandleLoadedAsync()
        {
            int[] allowedWarehouseTypeIds =
            {
                WarehouseKind.MainId, WarehouseKind.PickupId, WarehouseKind.ShowCaseId, WarehouseKind.ServiceId, WarehouseKind.AssemblyId
            };

            Task<PagedResult<WarehouseDto>> warehousesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);
            Task<PagedResult<ContractorDto>> suppliersTask = WebClient.ExecuteApiRequestAsync(new QueryContractors(true, anySupplierWarehouses: true));

            await Task.WhenAll(warehousesTask, suppliersTask);

            AllWarehouses = warehousesTask.Result.Data.Where(x => x.Active > 0.5 && allowedWarehouseTypeIds.Contains(x.TypeId)).ToObservableCollection();
            AllSuppliers = suppliersTask.Result.Data.Where(x => x.IsSupplier && x.Active).Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableCollection();
            AllWarehouseTypeIds = Dictionaries
                .GetItems<WarehouseKind>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .Where(x => allowedWarehouseTypeIds.Contains(x.Id))
                .ToObservableCollection();

            SelectedWarehouseTypeIds = AllWarehouseTypeIds.Cast<object>().ToList();
            SelectedWarehouses = AllWarehouses.Cast<object>().ToList();
            SelectedSuppliers = AllSuppliers.Cast<object>().ToList();

            await RefreshAsync();

            SelectPanTool();
        }

        private static DiagramConnector CreateConnector(
            IDiagramItem beginDiagramItem,
            DiagramItem endDiagramItem,
            LogisticsMapEntity entity,
            int entityId,
            bool main,
            bool rightAngleConnectorType = false,
            int endItemPointIndex = -1,
            int beginItemPointIndex = -1)
        {
            DiagramPointCollection diagramPointCollection = null;
            ConnectorType type;

            if (Math.Abs(beginDiagramItem.Position.X - endDiagramItem.Position.X) < 0.01 && Math.Abs(beginDiagramItem.Position.Y - endDiagramItem.Position.Y) > 120)
            {
                double yPointPosition = (beginDiagramItem.Position.Y + endDiagramItem.Position.Y) / 2;

                type = ConnectorType.Curved;
                endItemPointIndex = 1;
                beginItemPointIndex = 1;
                diagramPointCollection = new DiagramPointCollection(new[] { new Point(beginDiagramItem.Position.X + new Random().Next(150, 420), yPointPosition) });
            }
            else
            {
                type = ConnectorType.Straight;
            }

            if (rightAngleConnectorType)
            {
                type = ConnectorType.RightAngle;
            }

            DiagramItemContext beginDiagramItemContext = beginDiagramItem.DataContext as DiagramItemContext;
            DiagramItemContext endDiagramItemContext = endDiagramItem.DataContext as DiagramItemContext;

            return new DiagramConnector()
            {
                Stroke = endDiagramItem.Background,
                AllowDrop = false,
                CanMove = false,
                Focusable = false,
                CanDelete = false,
                CanEdit = false,
                CanRotate = false,
                CanSelect = true,
                CanChangeRoute = false,
                CanChangeParent = false,
                CanSnapToOtherItems = false,
                CanSnapToThisItem = false,
                CanDragEndPoint = false,
                BeginItem = beginDiagramItem,
                EndItem = endDiagramItem,
                StrokeThickness = main ? 2 : 1,
                Type = type,
                EndItemPointIndex = endItemPointIndex,
                BeginItemPointIndex = beginItemPointIndex,
                Points = diagramPointCollection,
                DataContext = new DiagramItemContext(entityId, entity, $"Маршрут {beginDiagramItemContext!.Text} => {endDiagramItemContext!.Text}")
            };
        }

        private static DiagramShape CreateSupplierWarehouseShape(
            int supplierId,
            string supplierName,
            string supplierWarehouseName,
            string cityName,
            double yPreviowsPosition)
        {
            return new DiagramShape()
            {
                Position = new Point(SupplierWarehouseXPosition, yPreviowsPosition + 65),
                Width = 120,
                Height = 60,
                AllowDrop = false,
                Focusable = false,
                Content = $"{supplierName}\r\n{supplierWarehouseName}",
                FontSize = 14,
                Shape = BasicShapes.RoundCornerRectangle,
                Foreground = Brushes.Black,
                Background = Brushes.Gray,
                BorderThickness = new Thickness(2, 2, 2, 2),
                CanMove = true,
                CanSelect = true,
                CanEdit = false,
                DataContext = new DiagramItemContext(supplierId, LogisticsMapEntity.SupplierWarehouse, $"{supplierName} г.{cityName}, {supplierWarehouseName}")
            };
        }

        private DiagramShape CreateMainWarehouseShape(int warehouseId, string warehouseName, string colorHex, double yPreviowsPosition, double xPosition)
        {
            return new DiagramShape()
            {
                Position = new Point(xPosition, yPreviowsPosition + 105),
                Width = 120,
                Height = 80,
                AllowDrop = false,
                Focusable = false,
                Content = warehouseName,
                FontSize = 14,
                CanRotate = true,
                Shape = BasicShapes.RoundCornerRectangle,
                Foreground = Brushes.Black,
                Background = (Brush)_converter.ConvertFromString(colorHex),
                BorderThickness = new Thickness(2, 2, 2, 2),
                BorderBrush = Brushes.Black,
                CanMove = true,
                CanEdit = false,
                CanSelect = true,
                DataContext = new DiagramItemContext(warehouseId, LogisticsMapEntity.Warehouse, warehouseName)
            };
        }

        private DiagramShape CreateShowcaseWarehouseShape(int warehouseId, string warehouseName, string colorHex, double yPreviowsPosition, double xPosition)
        {
            return new DiagramShape()
            {
                Position = new Point(xPosition, yPreviowsPosition + 85),
                Width = 120,
                Height = 60,
                AllowDrop = false,
                Focusable = false,
                Content = warehouseName,
                CanRotate = true,
                FontSize = 14,
                CanSelect = true,
                Shape = BasicShapes.RoundCornerRectangle,
                Foreground = Brushes.Black,
                Background = (Brush)_converter.ConvertFromString(colorHex),
                BorderThickness = new Thickness(2, 2, 2, 2),
                CanMove = true,
                CanEdit = false,
                DataContext = new DiagramItemContext(warehouseId, LogisticsMapEntity.Warehouse, warehouseName)
            };
        }

        private DiagramShape CreateAssemblyWarehouseShape(int warehouseId, string warehouseName, string colorHex, double yPreviowsPosition, double xPosition)
        {
            return new DiagramShape()
            {
                Position = new Point(xPosition, yPreviowsPosition + 85),
                Width = 120,
                Height = 60,
                AllowDrop = false,
                Focusable = false,
                Content = warehouseName,
                CanRotate = true,
                FontSize = 14,
                CanSelect = true,
                Shape = BasicShapes.RoundCornerRectangle,
                Foreground = Brushes.Black,
                Background = (Brush)_converter.ConvertFromString(colorHex),
                BorderThickness = new Thickness(2, 2, 2, 2),
                CanMove = true,
                CanEdit = false,
                DataContext = new DiagramItemContext(warehouseId, LogisticsMapEntity.Warehouse, warehouseName)
            };
        }

        private DiagramShape CreateServiceCenterShape(
            int serviceCenterId,
            string serviceCenterName,
            string serviceCenterAddress,
            int serviceCenterTypeId,
            double yPreviowsPosition,
            double xPosition)
        {
            return new DiagramShape()
            {
                Position = new Point(xPosition, yPreviowsPosition + 65),
                Width = 120,
                Height = 60,
                AllowDrop = false,
                Focusable = false,
                Content = serviceCenterName,
                CanRotate = true,
                FontSize = 14,
                CanSelect = true,
                Shape = BasicShapes.RoundCornerRectangle,
                Foreground = Brushes.Black,
                Background = Brushes.LightSalmon,
                BorderThickness = new Thickness(20, 20, 20, 20),
                CanMove = true,
                CanEdit = false,
                BorderBrush = serviceCenterTypeId == ServiceCenterType.SupplierId ? _supplierWarehouseBackground : null,
                DataContext = new DiagramItemContext(serviceCenterId, LogisticsMapEntity.ServiceCenter, $"{serviceCenterName} ({serviceCenterAddress})")
            };
        }

        private async Task RefreshAsync()
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.Show();

            if (SelectedWarehouses?.Any() != true)
            {
                MessageFacadeService.ShowNotificationError("Не выбран ни один склад");
                return;
            }

            LogisticsMapFilteringItem filter = new LogisticsMapFilteringItem()
            {
                WarehouseIds = SelectedWarehouses.Cast<WarehouseDto>().Select(x => x.Id).ToArray(),
                SupplierIds = SelectedSuppliers?.Cast<ComboBoxItem>().Select(x => x.Id).ToArray()
            };

            LogisticsMapDto logisticsMapDto = await WebClient.ExecuteApiRequestAsync(new QueryLogisticsMap(filter));

            GenerateDiagram(logisticsMapDto);

            splashScreenManager.Close();
        }

        private void GenerateDiagram(LogisticsMapDto logisticsMapDto)
        {
            OnDiagramCleared.Invoke(this, null!);

            const double supplierWarehouseYPosition = 20;
            const double margin = 400;
            double mainWarehouseYPosition = 50;
            double assemblyWarehouseYPosition = 50;
            double showcaseWarehouseYPosition = 20;
            double serviceWarehouseYPosition = 20;
            double serviceCenterYPosition = 20;

            HashSet<int> serviceCenterIds = WithoutLogistics
                ? logisticsMapDto.ServiceCenters.Select(x => x.Id).ToHashSet()
                : logisticsMapDto.WarehouseLogistics
                    .SelectMany(x => x.ServiceCenterRoutes)
                    .Select(x => x.ServiceCenterId)
                    .ToHashSet();

            int supplierWarehousesCount = logisticsMapDto.SupplierLogistics.SelectMany(x => x.Warehouses).Where(SupplierWarehouseFilter).Count();
            int warehousesCount = logisticsMapDto.WarehouseLogistics.Where(WarehouseFilter).Count();
            int serviceCentersCount = serviceCenterIds.Count;

            double mainWarehousesXPosition = margin + (supplierWarehousesCount * SelectedWidthCoefficient);
            double assemblyWarehousesXPosition = margin + mainWarehousesXPosition + (supplierWarehousesCount * SelectedWidthCoefficient);
            double showcaseWarehousesXPosition = margin + assemblyWarehousesXPosition + (warehousesCount * SelectedWidthCoefficient);
            double clientXPosition = margin + showcaseWarehousesXPosition + (warehousesCount * SelectedWidthCoefficient);
            double serviceWarehousesXPosition = margin + clientXPosition + (warehousesCount * SelectedWidthCoefficient);
            double serviceCentersXPosition = 250 + serviceWarehousesXPosition + (serviceCentersCount * SelectedWidthCoefficient);

            Dictionary<int, IDiagramItem> supplierWarehousesShapes = new Dictionary<int, IDiagramItem>();
            Dictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes = new Dictionary<(int entityId, LogisticsMapEntity entity), DiagramItem>();

            IReadOnlyCollection<WarehouseLogisticsDto> mainWarehouses = logisticsMapDto.WarehouseLogistics
                .Where(x => x.WarehouseTypeId == WarehouseKind.MainId && WarehouseFilter(x))
                .ToArray();

            IReadOnlyCollection<WarehouseLogisticsDto> assemblyWarehouses = logisticsMapDto.WarehouseLogistics
                .Where(x => x.WarehouseTypeId == WarehouseKind.AssemblyId && WarehouseFilter(x))
                .ToArray();

            IReadOnlyCollection<WarehouseLogisticsDto> salesWarehouses = logisticsMapDto.WarehouseLogistics
                .Where(x => x.WarehouseTypeId is WarehouseKind.PickupId or WarehouseKind.ShowCaseId && WarehouseFilter(x))
                .ToArray();

            IReadOnlyCollection<WarehouseLogisticsDto> serviceWarehouses = logisticsMapDto.WarehouseLogistics
                .Where(x => x.WarehouseTypeId == WarehouseKind.ServiceId && WarehouseFilter(x))
                .ToArray();

            List<SupplierWarehouseLogisticsDto> supplierWarehousesWithShapes = new List<SupplierWarehouseLogisticsDto>();

            CreateSupplierWarehousesShapes(logisticsMapDto, supplierWarehousesWithShapes, supplierWarehouseYPosition, supplierWarehousesShapes);

            CreateMainWarehousesShapes(shapes, mainWarehouses, ref mainWarehouseYPosition, mainWarehousesXPosition);

            assemblyWarehouseYPosition = mainWarehouseYPosition + 25;

            CreateAssemblyWarehousesShapes(shapes, assemblyWarehouses, ref assemblyWarehouseYPosition, assemblyWarehousesXPosition);

            CreateSaleWarehousesShapes(shapes, logisticsMapDto, ref showcaseWarehouseYPosition, showcaseWarehousesXPosition);

            CreateServiceWarehousesShapes(shapes, logisticsMapDto, ref serviceWarehouseYPosition, serviceWarehousesXPosition);

            CreateServiceCentersShapes(shapes, logisticsMapDto, ref serviceCenterYPosition, serviceCentersXPosition, serviceCenterIds);

            CreateVirtualClientShapeWithConnections(logisticsMapDto.WarehouseLogistics, shapes, clientXPosition);

            ConnectSupplierWarehousesWithOtherWarehouses(supplierWarehousesWithShapes, supplierWarehousesShapes, shapes);

            ConnectMainWarehousesWithOtherWarehouses(mainWarehouses, shapes);

            ConnectAssemblyWarehousesWithOtherWarehouses(assemblyWarehouses, shapes);

            ConnectSaleWarehousesWithOtherWarehouses(salesWarehouses, shapes);

            ConnectServiceWarehousesWithOtherWarehouses(serviceWarehouses, shapes);

            ConnectServiceWarehousesWithServiceCenters(serviceWarehouses, shapes);
        }

        private void CreateVirtualClientShapeWithConnections(
            IReadOnlyCollection<WarehouseLogisticsDto> allWarehouses,
            IReadOnlyDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes,
            double clientXPosition)
        {
            DiagramShape clientShape = CreateClientShape(clientXPosition);

            OnDiagramShapeAdded.Invoke(this, clientShape);

            foreach (WarehouseLogisticsDto warehouse in allWarehouses.Where(x => x.AnyDeliveries))
            {
                if (!shapes.TryGetValue((warehouse.WarehouseId, LogisticsMapEntity.Warehouse), out DiagramItem warehouseShape))
                {
                    continue;
                }

                DiagramConnector connector = CreateConnector(
                    warehouseShape,
                    clientShape,
                    LogisticsMapEntity.ClientRoute,
                    0,
                    true);

                OnDiagramConnectorAdded.Invoke(this, connector);
            }
        }

        private void CreateSupplierWarehousesShapes(
            LogisticsMapDto logisticsMapDto,
            ICollection<SupplierWarehouseLogisticsDto> supplierWarehousesWithShapes,
            double supplierWarehouseYPosition,
            IDictionary<int, IDiagramItem> supplierWarehousesShapes)
        {
            foreach (SupplierLogisticsDto supplierLogistics in logisticsMapDto.SupplierLogistics)
            {
                SupplierWarehouseLogisticsDto[] filteredWarehouses = supplierLogistics.Warehouses.Where(SupplierWarehouseFilter).ToArray();

                if (filteredWarehouses.Length == 0)
                {
                    continue;
                }

                foreach (SupplierWarehouseLogisticsDto supplierWarehouse in filteredWarehouses)
                {
                    DiagramShape supplierWarehouseShape = CreateSupplierWarehouseShape(
                        supplierLogistics.ContractorId,
                        supplierLogistics.ContractorName,
                        supplierWarehouse.Name,
                        supplierWarehouse.CityName,
                        supplierWarehouseYPosition);

                    supplierWarehousesShapes[supplierWarehouse.Id] = supplierWarehouseShape;

                    supplierWarehouseYPosition = supplierWarehouseShape.Position.Y;

                    OnDiagramShapeAdded.Invoke(this, supplierWarehouseShape);

                    supplierWarehousesWithShapes.Add(supplierWarehouse);
                }

                supplierWarehouseYPosition += 30;
            }
        }

        private void CreateMainWarehousesShapes(
            IDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes,
            IReadOnlyCollection<WarehouseLogisticsDto> mainWarehouses,
            ref double mainWarehouseYPosition,
            double mainWarehousesXPosition)
        {
            foreach (WarehouseLogisticsDto warehouseLogistics in mainWarehouses)
            {
                if (warehouseLogistics.WarehouseId == Constants.MainKyivWarehouseId)
                {
                    _mainKyivWarehouseYPosition = mainWarehouseYPosition;
                }

                DiagramShape mainWarehouseShape = CreateMainWarehouseShape(
                    warehouseLogistics.WarehouseId,
                    warehouseLogistics.WarehouseName,
                    warehouseLogistics.ColorHex,
                    mainWarehouseYPosition,
                    mainWarehousesXPosition);

                shapes[(warehouseLogistics.WarehouseId, LogisticsMapEntity.Warehouse)] = mainWarehouseShape;

                mainWarehouseYPosition = mainWarehouseShape.Position.Y;

                OnDiagramShapeAdded.Invoke(this, mainWarehouseShape);
            }
        }

        private void CreateAssemblyWarehousesShapes(
            IDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes,
            IReadOnlyCollection<WarehouseLogisticsDto> assemblyWarehouses,
            ref double assemblyWarehouseYPosition,
            double mainWarehousesXPosition)
        {
            foreach (WarehouseLogisticsDto warehouseLogistics in assemblyWarehouses)
            {
                DiagramShape assemblyWarehouseShape = CreateAssemblyWarehouseShape(
                    warehouseLogistics.WarehouseId,
                    warehouseLogistics.WarehouseName,
                    warehouseLogistics.ColorHex,
                    assemblyWarehouseYPosition,
                    mainWarehousesXPosition);

                shapes[(warehouseLogistics.WarehouseId, LogisticsMapEntity.Warehouse)] = assemblyWarehouseShape;

                assemblyWarehouseYPosition = assemblyWarehouseShape.Position.Y;

                OnDiagramShapeAdded.Invoke(this, assemblyWarehouseShape);
            }
        }

        private void CreateSaleWarehousesShapes(
            IDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> warehousesShapes,
            LogisticsMapDto logisticsMapDto,
            ref double showcaseWarehouseYPosition,
            double showcasesXPosition)
        {
            foreach (IGrouping<string, WarehouseLogisticsDto> warehouseLogisticsAddressGroup in logisticsMapDto.WarehouseLogistics
                         .Where(x => x.WarehouseTypeId is WarehouseKind.ShowCaseId or WarehouseKind.PickupId && WarehouseFilter(x))
                         .GroupBy(x => x.WarehouseAddress)
                         .OrderBy(x => x.Key))
            {
                foreach (WarehouseLogisticsDto warehouseLogistics in warehouseLogisticsAddressGroup)
                {
                    DiagramShape showcaseWarehouseShape = CreateShowcaseWarehouseShape(
                        warehouseLogistics.WarehouseId,
                        warehouseLogistics.WarehouseName,
                        warehouseLogistics.ColorHex,
                        showcaseWarehouseYPosition,
                        showcasesXPosition);

                    warehousesShapes[(warehouseLogistics.WarehouseId, LogisticsMapEntity.Warehouse)] = showcaseWarehouseShape;

                    showcaseWarehouseYPosition = showcaseWarehouseShape.Position.Y;

                    OnDiagramShapeAdded.Invoke(this, showcaseWarehouseShape);
                }

                showcaseWarehouseYPosition += 15;
            }
        }

        private DiagramShape CreateClientShape(double xPosition)
        {
            return new DiagramShape()
            {
                Position = new Point(xPosition, _mainKyivWarehouseYPosition),
                Width = 120,
                Height = 60,
                AllowDrop = false,
                Focusable = false,
                Content = "Клиент",
                CanRotate = true,
                FontSize = 14,
                CanSelect = true,
                Shape = BasicShapes.RoundCornerRectangle,
                Foreground = Brushes.Black,
                Background = (Brush)_converter.ConvertFromString("#5893E0"),
                BorderThickness = new Thickness(2, 2, 2, 2),
                CanMove = true,
                DataContext = new DiagramItemContext(0, LogisticsMapEntity.Client, "Клиент")
            };
        }

        private void CreateServiceWarehousesShapes(
            IDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> warehousesShapes,
            LogisticsMapDto logisticsMapDto,
            ref double serviceWarehouseYPosition,
            double serviceWarehousesXPosition)
        {
            foreach (IGrouping<string, WarehouseLogisticsDto> warehouseLogisticsAddressGroup in logisticsMapDto.WarehouseLogistics
                         .Where(x => x.WarehouseTypeId == WarehouseKind.ServiceId && WarehouseFilter(x))
                         .GroupBy(x => x.WarehouseAddress)
                         .OrderBy(x => x.Key))
            {
                foreach (WarehouseLogisticsDto warehouseLogistics in warehouseLogisticsAddressGroup)
                {
                    DiagramShape serviceWarehouseShape = CreateShowcaseWarehouseShape(
                        warehouseLogistics.WarehouseId,
                        warehouseLogistics.WarehouseName,
                        warehouseLogistics.ColorHex,
                        serviceWarehouseYPosition,
                        serviceWarehousesXPosition);

                    warehousesShapes[(warehouseLogistics.WarehouseId, LogisticsMapEntity.Warehouse)] = serviceWarehouseShape;

                    serviceWarehouseYPosition = serviceWarehouseShape.Position.Y;

                    OnDiagramShapeAdded.Invoke(this, serviceWarehouseShape);
                }

                serviceWarehouseYPosition += 10;
            }
        }

        private void CreateServiceCentersShapes(
            IDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes,
            LogisticsMapDto logisticsMapDto,
            ref double serviceCentersYPosition,
            double serviceCentersXPosition,
            IReadOnlySet<int> serviceCenterIds)
        {
            foreach (IGrouping<int, ServiceCenterDto> serviceCentersGroup in logisticsMapDto.ServiceCenters
                         .Where(x => serviceCenterIds.Contains(x.Id))
                         .GroupBy(x => x.CityId))
            {
                foreach (ServiceCenterDto serviceCenter in serviceCentersGroup)
                {
                    DiagramShape serviceCenterShape = CreateServiceCenterShape(
                        serviceCenter.Id,
                        serviceCenter.Name,
                        serviceCenter.Address,
                        serviceCenter.TypeId,
                        serviceCentersYPosition,
                        serviceCentersXPosition);

                    shapes[(serviceCenter.Id, LogisticsMapEntity.ServiceCenter)] = serviceCenterShape;

                    serviceCentersYPosition = serviceCenterShape.Position.Y;

                    OnDiagramShapeAdded.Invoke(this, serviceCenterShape);
                }

                serviceCentersYPosition += 20;
            }
        }

        private void ConnectSupplierWarehousesWithOtherWarehouses(
            IReadOnlyCollection<SupplierWarehouseLogisticsDto> supplierWarehousesWithShapes,
            IReadOnlyDictionary<int, IDiagramItem> supplierWarehousesShapes,
            IReadOnlyDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes)
        {
            foreach (SupplierWarehouseLogisticsDto supplierWarehouse in supplierWarehousesWithShapes.Where(x => x.Carries?.Any(SupplierCarryFilter) == true))
            {
                IDiagramItem fromSupplierWarehouseShape = supplierWarehousesShapes[supplierWarehouse.Id];

                DiagramItemContext fromDiagramItemContext = fromSupplierWarehouseShape.DataContext as DiagramItemContext;

                foreach (SupplierCarryDto supplierWarehouseCarry in supplierWarehouse.Carries.Where(SupplierCarryFilter))
                {
                    if (!shapes.TryGetValue((supplierWarehouseCarry.WarehouseId, LogisticsMapEntity.Warehouse), out DiagramItem toWarehouseShape))
                    {
                        continue;
                    }

                    DiagramConnector connector = CreateConnector(
                        fromSupplierWarehouseShape,
                        toWarehouseShape,
                        LogisticsMapEntity.SupplierWarehouseRoute,
                        fromDiagramItemContext!.EntityId,
                        true);

                    OnDiagramConnectorAdded.Invoke(this, connector);
                }
            }
        }

        private void ConnectMainWarehousesWithOtherWarehouses(
            IReadOnlyCollection<WarehouseLogisticsDto> mainWarehouses,
            IReadOnlyDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes)
        {
            foreach (WarehouseLogisticsDto mainWarehouse in mainWarehouses.Where(x => x.ToRoutes?.Any() == true))
            {
                IDiagramItem fromWarehouseShape = shapes[(mainWarehouse.WarehouseId, LogisticsMapEntity.Warehouse)];

                foreach (WarehouseRouteSimpleDto fromCurrentWarehouseRoute in mainWarehouse.FromRoutes)
                {
                    if (!shapes.TryGetValue((fromCurrentWarehouseRoute.WarehouseToId, LogisticsMapEntity.Warehouse), out DiagramItem toWarehouseShape))
                    {
                        continue;
                    }

                    DiagramConnector connector = CreateConnector(
                        fromWarehouseShape,
                        toWarehouseShape,
                        LogisticsMapEntity.WarehouseRoute,
                        fromCurrentWarehouseRoute.WarehouseFromId,
                        true);

                    OnDiagramConnectorAdded.Invoke(this, connector);
                }
            }
        }

        private void ConnectAssemblyWarehousesWithOtherWarehouses(
            IReadOnlyCollection<WarehouseLogisticsDto> assemblyWarehouses,
            IReadOnlyDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes)
        {
            foreach (WarehouseLogisticsDto assemblyWarehouse in assemblyWarehouses.Where(x => x.ToRoutes?.Any() == true))
            {
                IDiagramItem fromWarehouseShape = shapes[(assemblyWarehouse.WarehouseId, LogisticsMapEntity.Warehouse)];

                foreach (WarehouseRouteSimpleDto fromCurrentWarehouseRoute in assemblyWarehouse.FromRoutes)
                {
                    if (!shapes.TryGetValue((fromCurrentWarehouseRoute.WarehouseToId, LogisticsMapEntity.Warehouse), out DiagramItem toWarehouseShape))
                    {
                        continue;
                    }

                    DiagramConnector connector = CreateConnector(
                        fromWarehouseShape,
                        toWarehouseShape,
                        LogisticsMapEntity.WarehouseRoute,
                        fromCurrentWarehouseRoute.WarehouseFromId,
                        true);

                    OnDiagramConnectorAdded.Invoke(this, connector);
                }
            }
        }

        private void ConnectSaleWarehousesWithOtherWarehouses(
            IReadOnlyCollection<WarehouseLogisticsDto> saleWarehouses,
            IReadOnlyDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes)
        {
            foreach (WarehouseLogisticsDto saleWarehouse in saleWarehouses.Where(x => x.ToRoutes?.Any() == true))
            {
                IDiagramItem fromWarehouseShape = shapes[(saleWarehouse.WarehouseId, LogisticsMapEntity.Warehouse)];

                foreach (WarehouseRouteSimpleDto fromCurrentWarehouseRoute in saleWarehouse.FromRoutes)
                {
                    if (!shapes.TryGetValue((fromCurrentWarehouseRoute.WarehouseToId, LogisticsMapEntity.Warehouse), out DiagramItem toWarehouseShape))
                    {
                        continue;
                    }

                    DiagramConnector connector = CreateConnector(
                        fromWarehouseShape,
                        toWarehouseShape,
                        LogisticsMapEntity.WarehouseRoute,
                        fromCurrentWarehouseRoute.WarehouseFromId,
                        true);

                    OnDiagramConnectorAdded.Invoke(this, connector);
                }
            }
        }

        private void ConnectServiceWarehousesWithOtherWarehouses(
            IReadOnlyCollection<WarehouseLogisticsDto> serviceWarehouses,
            IReadOnlyDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes)
        {
            foreach (WarehouseLogisticsDto saleWarehouse in serviceWarehouses.Where(x => x.ToRoutes?.Any() == true))
            {
                IDiagramItem fromWarehouseShape = shapes[(saleWarehouse.WarehouseId, LogisticsMapEntity.Warehouse)];

                foreach (WarehouseRouteSimpleDto fromCurrentWarehouseRoute in saleWarehouse.FromRoutes)
                {
                    if (!shapes.TryGetValue((fromCurrentWarehouseRoute.WarehouseToId, LogisticsMapEntity.Warehouse), out DiagramItem toWarehouseShape))
                    {
                        continue;
                    }

                    DiagramConnector connector = CreateConnector(
                        fromWarehouseShape,
                        toWarehouseShape,
                        LogisticsMapEntity.WarehouseRoute,
                        fromCurrentWarehouseRoute.WarehouseFromId,
                        true);

                    OnDiagramConnectorAdded.Invoke(this, connector);
                }
            }
        }

        private void ConnectServiceWarehousesWithServiceCenters(
            IReadOnlyCollection<WarehouseLogisticsDto> serviceWarehouses,
            IReadOnlyDictionary<(int entityId, LogisticsMapEntity entity), DiagramItem> shapes)
        {
            foreach (WarehouseLogisticsDto saleWarehouse in serviceWarehouses.Where(x => x.ServiceCenterRoutes?.Any() == true))
            {
                IDiagramItem fromWarehouseShape = shapes[(saleWarehouse.WarehouseId, LogisticsMapEntity.Warehouse)];

                foreach (WarehouseServiceCenterRouteDto serviceCenterRoute in saleWarehouse.ServiceCenterRoutes)
                {
                    if (!shapes.TryGetValue((serviceCenterRoute.ServiceCenterId, LogisticsMapEntity.ServiceCenter), out DiagramItem serviceCenterShape))
                    {
                        continue;
                    }

                    DiagramConnector connector = CreateConnector(
                        fromWarehouseShape,
                        serviceCenterShape,
                        LogisticsMapEntity.WarehouseServiceCenterRoute,
                        serviceCenterRoute.Id,
                        true,
                        true,
                        beginItemPointIndex: 1);

                    OnDiagramConnectorAdded.Invoke(this, connector);
                }
            }
        }

        private void ResetFilter()
        {
            SelectedWarehouses = AllWarehouses.Cast<object>().ToList();
            SelectedSuppliers = AllSuppliers.Cast<object>().ToList();
            SelectedWidthCoefficient = DefaultWidthCoefficient;
        }

        private void OnSelectedWarehouseTypesChanged()
        {
            IReadOnlyCollection<int> typeIds;

            if (SelectedWarehouseTypeIds?.Any() == true)
            {
                typeIds = SelectedWarehouseTypeIds
                    .Cast<ComboBoxItem>()
                    .Select(x => x.Id)
                    .ToArray();
            }
            else
            {
                typeIds = Array.Empty<int>();
            }

            SelectedWarehouses = AllWarehouses
                .Where(x => typeIds.Contains(x.TypeId))
                .Cast<object>()
                .ToList();
        }

        private void DiagramDoubleClick(DiagramItem diagramItem)
        {
            if (diagramItem?.DataContext is not DiagramItemContext diagramItemContext)
            {
                return;
            }

            switch (diagramItemContext.Entity)
            {
                case LogisticsMapEntity.Warehouse:
                    SizeableDialogDocumentManagerService.ShowView<WarehouseViewModel>(new WarehouseEditParameter(diagramItemContext.EntityId), this);

                    return;
                case LogisticsMapEntity.SupplierWarehouse:
                    _messenger.Send(new ContractorViewMessage(diagramItemContext.EntityId)
                    {
                        OpenOnLogisticsTab = true
                    });

                    return;
                case LogisticsMapEntity.WarehouseRoute:

                    SizeableDialogDocumentManagerService.ShowView<WarehouseViewModel>(
                        new WarehouseEditParameter(diagramItemContext.EntityId)
                        {
                            OpenOnLogisticsTab = true
                        },
                        this);

                    return;
                case LogisticsMapEntity.SupplierWarehouseRoute:
                    _messenger.Send(new ContractorViewMessage(diagramItemContext.EntityId)
                    {
                        OpenOnLogisticsTab = true
                    });
                    return;
                case LogisticsMapEntity.ServiceCenter:
                    _messenger.Send(new ServiceCenterViewMessage(diagramItemContext.EntityId));

                    return;
                default:
                    MessageFacadeService.ShowNotificationWarning("Объект открыть нельзя");
                    return;
            }
        }

        private void DiagramSelectionChanged(DiagramItem diagramItem)
        {
            SelectedDiagramItem = diagramItem;

            if (diagramItem?.DataContext is DiagramItemContext diagramItemContext)
            {
                SelectedDiagramItemText = diagramItemContext.Text;
            }
            else
            {
                SelectedDiagramItemText = null;
            }
        }

        private bool SupplierWarehouseFilter(SupplierWarehouseLogisticsDto dto)
        {
            return WithoutLogistics || dto.Carries?.Any(SupplierCarryFilter) == true;
        }

        private bool WarehouseFilter(WarehouseLogisticsDto dto)
        {
            return WithoutLogistics || dto.FromRoutes?.Any() == true || dto.ToRoutes?.Any() == true || dto.ServiceCenterRoutes?.Any() == true;
        }

        private bool SupplierCarryFilter(SupplierCarryDto supplierCarryDto)
        {
            return !OnlyMainSupplierRoutes || supplierCarryDto.Main;
        }

        private void OnWithoutLogisticsChanged()
        {
            if (WithoutLogistics)
            {
                OnlyMainSupplierRoutes = false;
            }
        }

        private void OnOnlyMainSupplierRoutesChanged()
        {
            if (OnlyMainSupplierRoutes)
            {
                WithoutLogistics = false;
            }
        }

        private void SelectPanTool()
        {
            PanToolSelected.Invoke(this, null!);
            AllowSelectPanTool = false;
            AllowSelectPointerTool = true;
        }

        private void SelectPointerTool()
        {
            PointerToolSelected.Invoke(this, null!);
            AllowSelectPanTool = true;
            AllowSelectPointerTool = false;
        }
    }
}