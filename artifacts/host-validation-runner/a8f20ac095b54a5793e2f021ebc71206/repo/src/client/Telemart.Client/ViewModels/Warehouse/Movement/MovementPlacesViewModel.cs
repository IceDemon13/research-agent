using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public class MovementPlacesViewModel : TelemartDialogViewModelBase
    {
        public MovementPlacesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public MovementPlacesViewModel()
        {
        }

        #region INPC

        public int Places
        {
            get { return GetProperty(() => Places); }
            set { SetProperty(() => Places, value); }
        }

        public int MaxPlaces
        {
            get { return GetProperty(() => MaxPlaces); }
            private set { SetProperty(() => MaxPlaces, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<MovementPlacesViewModel> builder)
        {
            builder.Property(x => x.Places).MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            MovementPlacesParameter p = (MovementPlacesParameter)Parameter;

            Places = p.Places ?? 0;
            MaxPlaces = p.MaxPlaces;

            Title = "Количество мест";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}
