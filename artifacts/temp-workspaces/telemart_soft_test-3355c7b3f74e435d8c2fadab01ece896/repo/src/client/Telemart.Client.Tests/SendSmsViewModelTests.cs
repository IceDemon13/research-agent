using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Sms;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class SendSmsViewModelTests
    {
        [Fact]
        public void IsInheritedFromIDataErrorInfoTest()
        {
            Type[] interfaces = typeof(SendSmsViewModel).GetInterfaces();
            Assert.Contains(typeof(IDataErrorInfo), interfaces);
        }

        [Fact]
        public void IsInheritedFromIDocumentContentTest()
        {
            Type[] interfaces = typeof(SendSmsViewModel).GetInterfaces();
            Assert.Contains(typeof(IDocumentContent), interfaces);
        }

        [Fact]
        public void IsViewModelTest()
        {
            Assert.True(typeof(SendSmsViewModel).BaseType == typeof(TelemartDialogViewModelBase));
        }

        [Fact]
        public void PropertyChangedRaisedForPhone()
        {
            SendSmsViewModel viewModel = new SendSmsViewModel();

            bool phoneChangedEventRaised = false;

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals("Phone", StringComparison.Ordinal))
                {
                    phoneChangedEventRaised = true;
                }
            };

            viewModel.Phone = "0964565454";

            Assert.True(phoneChangedEventRaised);
        }

        [Fact]
        public void PropertyChangedRaisedForText()
        {
            SendSmsViewModel viewModel = new SendSmsViewModel();

            bool textChangedEventRaised = false;

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(viewModel.SmsText), StringComparison.Ordinal))
                {
                    textChangedEventRaised = true;
                }
            };

            viewModel.SmsText = "Test Test";

            Assert.True(textChangedEventRaised);
        }

        [Fact]
        public void SendSmsCommandInvokesIWebClientSendSmsMethodTest()
        {
            MockRepository mockRepository = new MockRepository(MockBehavior.Loose);
            Mock<IWebClient> webClientMock = mockRepository.Create<IWebClient>();
            Mock<IDocumentOwner> documentOwnerMock = mockRepository.Create<IDocumentOwner>();
            Mock<IMessageFacadeService> messageFacadeService = mockRepository.Create<IMessageFacadeService>();
            Mock<IErrorHandler> mockErrorHandler = mockRepository.Create<IErrorHandler>();

            SendSmsViewModel viewModel = new SendSmsViewModel(
                webClientMock.Object,
                new Dictionaries.Dictionaries(webClientMock.Object),
                messageFacadeService.Object,
                mockErrorHandler.Object,
                NullLogger<SendSmsViewModel>.Instance);

            viewModel.DocumentOwner = documentOwnerMock.Object;

            viewModel.Phone = "0966785656";
            viewModel.SmsText = "test";
            viewModel.Subdivision = Subdivision.Telemart;

            viewModel.OkCommand.Execute(null);

            mockErrorHandler.Verify(
                x => x.HandleErrorsAsync(
                It.IsAny<Func<CancellationToken, Task<Result<object>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ISupportServices>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<Result<object>, CancellationToken, Task>>(),
                It.IsAny<Func<Exception, CancellationToken, Task>>(),
                It.IsAny<bool>()),
                Times.Once);
        }
    }
}