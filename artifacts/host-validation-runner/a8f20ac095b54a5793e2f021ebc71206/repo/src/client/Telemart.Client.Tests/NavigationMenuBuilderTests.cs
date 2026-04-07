using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using Telemart.Client.Common.Navigation;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Common.ErrorHandling;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public sealed class NavigationMenuBuilderTests
    {
        [Fact]
        public void EmptyRolesTest()
        {
            Mock<IFiscalRegistrarClientFactory> fiscalFactoryMock = new Mock<IFiscalRegistrarClientFactory>();
            Mock<IServiceProvider> serviceProviderMock = new Mock<IServiceProvider>();
            Mock<ILogger<NavigationMenuBuilder>> loggerMock = new Mock<ILogger<NavigationMenuBuilder>>();

            fiscalFactoryMock.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<IFiscalRegistrarClient>(null)).Verifiable();

            NavigationMenuBuilder builder = new NavigationMenuBuilder(fiscalFactoryMock.Object, serviceProviderMock.Object, loggerMock.Object);

            List<NavigationMenuGroup> navigationMenuGroups = builder.BuildNavigationMenu(Array.Empty<string>(), Array.Empty<BusinessOperation>()).ToList();

            Assert.Equal(10, navigationMenuGroups.Count);

            Assert.True(navigationMenuGroups.All(x => x.NavItems.All(y => y.Allowed(Array.Empty<string>(), Array.Empty<BusinessOperation>()))));
        }

        [Fact]
        public void NullRolesTest()
        {
            Mock<IFiscalRegistrarClientFactory> fiscalFactoryMock = new Mock<IFiscalRegistrarClientFactory>();
            Mock<IServiceProvider> serviceProviderMock = new Mock<IServiceProvider>();
            Mock<ILogger<NavigationMenuBuilder>> loggerMock = new Mock<ILogger<NavigationMenuBuilder>>();

            fiscalFactoryMock.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<IFiscalRegistrarClient>(null)).Verifiable();

            NavigationMenuBuilder builder = new NavigationMenuBuilder(fiscalFactoryMock.Object, serviceProviderMock.Object, loggerMock.Object);

            List<NavigationMenuGroup> navigationMenuGroups = builder.BuildNavigationMenu(null, null).ToList();

            Assert.True(navigationMenuGroups.All(x => x.NavItems.All(y => y.Allowed(null, null))));
        }
    }
}