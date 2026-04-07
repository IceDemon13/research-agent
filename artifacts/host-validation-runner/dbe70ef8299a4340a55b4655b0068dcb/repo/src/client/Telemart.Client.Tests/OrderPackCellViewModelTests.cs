using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.WarehouseCell;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Warehouse.Cell;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common.RecognizeWarehouseCell;
using Telemart.Client.ViewModels.Store.Order;
using Xunit;

namespace Telemart.Client.Tests
{
    public class OrderPackCellViewModelTests
    {
        [Fact]
        public async Task HandleLoadedAsync_HydratesSelectedCellIdsFromExistingWarehouseCellsAsync()
        {
            List<WarehouseCellDto> existingCells = new List<WarehouseCellDto>
            {
                CreateCell(1, "Cell1"),
                CreateCell(2, "Cell2")
            };

            Mock<IWebClient> webClientMock = CreateWebClientMock(existingCells);
            OrderPackCellViewModel viewModel = CreateViewModel(webClientMock);

            InvokeNonPublic(viewModel, "OnParameterChanged", new OrderPackCellParameter(new[] { 1, 2 }, 7));
            await InvokeNonPublicTaskAsync(viewModel, "HandleLoadedAsync");

            Assert.Collection(
                viewModel.WarehouseCells,
                x => Assert.Equal(1, x.Id),
                x => Assert.Equal(2, x.Id));

            bool added = (bool)InvokeNonPublic(viewModel, "TryAddWarehouseCell", CreateCell(1, "Cell1"));

            Assert.False(added);
            Assert.Equal(2, viewModel.WarehouseCells.Count);
        }

        [Fact]
        public void RecognizeBarcodeViewModelOnFinished_AddsSuccessfulScanOnce_AndFlagsDuplicate()
        {
            OrderPackCellViewModel viewModel = CreateViewModel();
            InvokeNonPublic(viewModel, "SetWarehouseCells", new List<WarehouseCellDto>());

            WarehouseCellDto cell = CreateCell(5, "Cell5");
            RecognizeWarehouseCellBarcodeResultEventArgs first = RecognizeWarehouseCellBarcodeResultEventArgs.Found(cell, "barcode-1");
            RecognizeWarehouseCellBarcodeResultEventArgs second = RecognizeWarehouseCellBarcodeResultEventArgs.Found(cell, "barcode-1");

            InvokeNonPublic(viewModel, "RecognizeBarcodeViewModelOnFinished", null, first);
            InvokeNonPublic(viewModel, "RecognizeBarcodeViewModelOnFinished", null, second);

            Assert.Single(viewModel.WarehouseCells);
            Assert.Equal(5, viewModel.WarehouseCells[0].Id);
            Assert.Equal("Ячейка уже просканирована", second.ErrorText);
        }

        [Fact]
        public void DeleteCellCommand_KeepsTrackedSetAndWarehouseCellsInSync()
        {
            OrderPackCellViewModel viewModel = CreateViewModel();
            InvokeNonPublic(viewModel, "SetWarehouseCells", new List<WarehouseCellDto>());

            WarehouseCellDto cell = CreateCell(9, "Cell9");
            bool firstAdd = (bool)InvokeNonPublic(viewModel, "TryAddWarehouseCell", cell);

            viewModel.DeleteCellCommand.Execute(cell);

            bool secondAdd = (bool)InvokeNonPublic(viewModel, "TryAddWarehouseCell", CreateCell(9, "Cell9"));

            Assert.True(firstAdd);
            Assert.True(secondAdd);
            Assert.Single(viewModel.WarehouseCells);
            Assert.Equal(9, viewModel.WarehouseCells[0].Id);
        }

        [Fact]
        public void TryAddWarehouseCell_AddsUniqueCellToTrackedStateAndUiCollectionOnce()
        {
            OrderPackCellViewModel viewModel = CreateViewModel();
            InvokeNonPublic(viewModel, "SetWarehouseCells", new List<WarehouseCellDto>());

            bool firstAdd = (bool)InvokeNonPublic(viewModel, "TryAddWarehouseCell", CreateCell(11, "Cell11"));
            bool duplicateAdd = (bool)InvokeNonPublic(viewModel, "TryAddWarehouseCell", CreateCell(11, "Cell11"));

            Assert.True(firstAdd);
            Assert.False(duplicateAdd);
            Assert.Single(viewModel.WarehouseCells);
            Assert.Equal(11, viewModel.WarehouseCells[0].Id);
        }

        private static OrderPackCellViewModel CreateViewModel(Mock<IWebClient> webClientMock = null)
        {
            webClientMock ??= CreateWebClientMock(new List<WarehouseCellDto>());
            EnsureLoggerServiceProvider();

            return new OrderPackCellViewModel(
                webClientMock.Object,
                new Mock<IDictionaries>(MockBehavior.Loose).Object,
                new Mock<IMessageFacadeService>(MockBehavior.Loose).Object);
        }

        private static void EnsureLoggerServiceProvider()
        {
            if (TelemartViewModelBase.ServiceProvider != null)
            {
                return;
            }

            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            TelemartViewModelBase.ServiceProvider = services.BuildServiceProvider();
        }

        private static Mock<IWebClient> CreateWebClientMock(List<WarehouseCellDto> warehouseCells)
        {
            Mock<IWebClient> webClientMock = new Mock<IWebClient>(MockBehavior.Loose);
            webClientMock
                .Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryWarehouseCells>(), It.IsAny<bool>()))
                .ReturnsAsync(warehouseCells);

            return webClientMock;
        }

        private static WarehouseCellDto CreateCell(int id, string name)
        {
            return new WarehouseCellDto
            {
                Id = id,
                Name = name,
                Active = true,
                WarehouseId = 7
            };
        }

        private static object InvokeNonPublic(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return method.Invoke(target, args);
        }

        private static async Task InvokeNonPublicTaskAsync(object target, string methodName, params object[] args)
        {
            Task task = (Task)InvokeNonPublic(target, methodName, args);
            await task;
        }
    }
}
