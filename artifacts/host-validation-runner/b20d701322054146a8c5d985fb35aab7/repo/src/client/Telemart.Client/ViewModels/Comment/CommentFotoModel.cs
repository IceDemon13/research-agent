using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DynamicData;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.ImageEntities;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.ImageEntities;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Comment
{
    public class CommentFotoModel : TelemartDialogViewModelBase
    {
        public CommentFotoModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;

            Images = new ObservableCollection<ImageSource>();
        }

        public ObservableCollection<ImageSource> Images
        {
            get { return GetProperty(() => Images); }
            private set { SetProperty(() => Images, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        protected override async Task HandleLoadedAsync()
        {
            CommentFotoParameter parameter = (CommentFotoParameter)Parameter;

            int commentId = parameter.CommentId;

            Title = $"Фото комментария №{commentId}";

            await LoadImageEntitiesAsync(commentId);
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }

        private async Task LoadImageEntitiesAsync(int commentId)
        {
            Images.Clear();

            List<ImageEntityDto> imageEntityDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryImageEntities(new ImageEntitiesFilter(ImageEntityType.Comment.Name, commentId))),
                "получении фото комментарий",
                null,
                this,
                true,
                showNotification: false);

            if (imageEntityDtos?.Count > 0)
            {
                List<BitmapImage> bitmapImages = new List<BitmapImage>();

                foreach (ImageEntityDto dto in imageEntityDtos.OrderBy(x => x.Position))
                {
                    // todo сохранение картинок зависит в комментов зависит от Вадика. Еще неизвестно точно как он будет их хранить.
                    byte[] imageBytes = await FileHelper.ReadBytesFromUrlAsync($"https://{dto.Url}");

                    bitmapImages.Add(GetImage(imageBytes));
                }

                Images.AddRange(bitmapImages);
            }
        }

        private BitmapImage GetImage(byte[] imageBytes)
        {
            BitmapImage image = new BitmapImage();

            using (Stream stream = new MemoryStream(imageBytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }

            return image;
        }
    }
}