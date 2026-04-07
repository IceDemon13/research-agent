using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.Product;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    internal class InvoiceProductsDimensionsViewModel : TelemartDialogViewModelBase
    {
        public InvoiceProductsDimensionsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.UseQuantity = false;

            Mapper = mapper;

            messenger.Register<ProductDimensionsMessage>(this, OnProductDimensionsMessage);
        }

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        public ReadOnlyObservableCollection<ProductDimensionsViewItem> Dimensions
        {
            get { return GetProperty(() => Dimensions); }
            set { SetProperty(() => Dimensions, value); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        private IMapper Mapper { get; }

        public override void OnDestroy()
        {
            RecognizeBarcodeViewModel.OnStarted -= RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished -= RecognizeBarcodeViewModelOnFinished;

            base.OnDestroy();
        }

        protected override Task HandleLoadedAsync()
        {
            List<ProductAttributesDto> products = (List<ProductAttributesDto>)Parameter;

            Dimensions = products.Select(x => Mapper.Map<ProductDimensionsViewItem>(x)).ToReadOnlyObservableCollection();

            RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), products);

            Title = "Внесение ВГХ товаров";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            if (Dimensions.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationWarning("Внесите ВГХ по всем товарам");
            }
            else
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            IsRecognitionInProgress = false;

            if (e.Result == RecognizeBarcodeResult.Found)
            {
                if (e.Product.DimmensionsIsValid())
                {
                    MessageFacadeService.ShowNotificationWarning("У выбранного товара уже заданы ВГХ");
                }
                else
                {
                    ProductDimensionsViewItem item = Mapper.Map<ProductDimensionsViewItem>(e.Product);
                    DialogDocumentManagerService.ShowView<ProductDimensionsViewModel>(item, this);
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationError("Просканированный товар отсутствует в накладной или не был обнаружен при сверке");
            }
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private void OnProductDimensionsMessage(ProductDimensionsMessage message)
        {
            if (message.MessageType == MessageType.Changed)
            {
                Dimensions.DoActionWithItem(x => x.Id == message.Entity.Id, x => Mapper.Map(message.Entity, x));
            }
        }
    }
}
