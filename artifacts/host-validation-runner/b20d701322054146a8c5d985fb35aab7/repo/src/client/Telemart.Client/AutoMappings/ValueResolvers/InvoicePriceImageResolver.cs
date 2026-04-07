using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoMapper;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class InvoicePriceImageResolver : IValueResolver<InvoiceDto, InvoiceViewItem, ImageSource>
    {
        private readonly ImageSource errorImage;
        private readonly ImageSource okImage;
        private readonly ImageSource warningImage;

        public InvoicePriceImageResolver(IWebClient webClient)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));

            okImage = new BitmapImage(new Uri(@"pack://application:,,,/Images/ok.png"));
            warningImage = new BitmapImage(new Uri(@"pack://application:,,,/Images/warning.png"));
            errorImage = new BitmapImage(new Uri(@"pack://application:,,,/Images/error.png"));
        }

        private IWebClient WebClient { get; }

        public ImageSource Resolve(InvoiceDto source, InvoiceViewItem destination, ImageSource destMember, ResolutionContext context)
        {
            if (source == null || source.InvoiceProducts == null)
            {
                return null;
            }

            int currentUserId = WebClient.AuthenticatedEmployee.Id;

            ImageSource priceImage;

            if (source.InvoiceProducts.Any())
            {
                priceImage = okImage;

                foreach (InvoiceProductDto invoiceProduct in source.InvoiceProducts)
                {
                    bool belongsToCurrentUser = currentUserId == invoiceProduct.EmployeeId || currentUserId == invoiceProduct.ProductEmployeeId;

                    if (invoiceProduct.Price.Equals(0) || invoiceProduct.Quantity < invoiceProduct.OrderQuantity)
                    {
                        priceImage = belongsToCurrentUser ? errorImage : warningImage;

                        if (belongsToCurrentUser)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                priceImage = warningImage;
            }

            return priceImage;
        }
    }
}
