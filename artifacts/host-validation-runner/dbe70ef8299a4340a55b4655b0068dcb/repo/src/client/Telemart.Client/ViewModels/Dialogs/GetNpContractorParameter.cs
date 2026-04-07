namespace Telemart.Client.ViewModels.Dialogs
{
    public class GetNpContractorParameter
    {
        public GetNpContractorParameter(string npContractorRef)
        {
            NpContractorRef = npContractorRef;
        }

        public string NpContractorRef { get; }
    }
}
