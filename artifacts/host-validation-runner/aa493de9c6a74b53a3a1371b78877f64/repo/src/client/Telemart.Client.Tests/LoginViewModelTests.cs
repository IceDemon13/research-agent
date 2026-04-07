using System;
using System.Collections.Generic;
using DevExpress.Mvvm;
using Moq;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Requests.Features.BonusType;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.Requests.Features.Sms;
using Telemart.Client.Data.Requests.Features.Warranty;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Payments;
using Telemart.Client.ViewModels;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class LoginViewModelTests
    {
        [Fact]
        public void AuthenticateCommandTest()
        {
            MockRepository mockRepository = new MockRepository(MockBehavior.Loose);

            Mock<IWebClient> webClientMock = mockRepository.Create<IWebClient>();
            webClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<AuthRequest>())).ReturnsAsync(new AuthResponse(0));
            webClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<AuthRequest>())).ReturnsAsync(new AuthResponse(0));
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryCarries>(), It.IsAny<bool>())).ReturnsAsync(new List<CarryDto>());
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryPayments>(), It.IsAny<bool>())).ReturnsAsync(new List<PaymentDto>());
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryBonusTypes>(), It.IsAny<bool>())).ReturnsAsync(new List<BonusTypeDto>());
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryWarranties>(), It.IsAny<bool>())).ReturnsAsync(new PagedResult<WarrantyDto> { Data = new List<WarrantyDto>() });
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QuerySmsTemplates>(), It.IsAny<bool>())).ReturnsAsync(new List<SmsTemplateDto>());
            webClientMock.Setup(x => x.ExecuteApiRequestAsync(It.IsAny<QueryInvoiceAdditionalCostSources>(), It.IsAny<bool>())).ReturnsAsync(new List<InvoiceAdditionalCostSourceDto>());

            Mock<IMessenger> messengerMock = mockRepository.Create<IMessenger>();
            Mock<IMessageFacadeService> messageFacadeServiceMock = mockRepository.Create<IMessageFacadeService>();

            LoginViewModel viewModel = new LoginViewModel(
                webClientMock.Object,
                messengerMock.Object,
                messageFacadeServiceMock.Object);

            viewModel.Login = "test";
            viewModel.Password = "test";

            viewModel.AuthenticateCommand.Execute(null);

            webClientMock.Verify(x => x.AuthenticateAsync(It.IsAny<AuthRequest>()), Times.Once);
        }

        [Fact]
        public void IsViewModelTest()
        {
            Assert.True(typeof(LoginViewModel).BaseType == typeof(ViewModelBase));
        }

        [Fact]
        public void PropertyChangedRaisedForErrorText()
        {
            LoginViewModel viewModel = new LoginViewModel();

            bool eventRaised = false;

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(viewModel.ErrorText), StringComparison.Ordinal))
                {
                    eventRaised = true;
                }
            };

            viewModel.ErrorText = "error";

            Assert.True(eventRaised);
        }

        [Fact]
        public void PropertyChangedRaisedForLogin()
        {
            LoginViewModel viewModel = new LoginViewModel();

            bool eventRaised = false;

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(viewModel.Login), StringComparison.Ordinal))
                {
                    eventRaised = true;
                }
            };

            viewModel.Login = "test";

            Assert.True(eventRaised);
        }

        [Fact]
        public void PropertyChangedRaisedForPassword()
        {
            LoginViewModel viewModel = new LoginViewModel();

            bool eventRaised = false;

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(viewModel.Password), StringComparison.Ordinal))
                {
                    eventRaised = true;
                }
            };

            viewModel.Password = "test";

            Assert.True(eventRaised);
        }
    }
}