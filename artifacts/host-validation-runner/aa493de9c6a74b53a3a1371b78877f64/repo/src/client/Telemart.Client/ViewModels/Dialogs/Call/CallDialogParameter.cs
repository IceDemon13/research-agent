namespace Telemart.Client.ViewModels.Dialogs.Call
{
    public class CallDialogParameter
    {
        public CallDialogParameter(int? callId, string fio, bool autoCall, string ivr, params string[] phones)
        {
            CallId = callId;
            Fio = fio;
            Phones = phones;
            AutoCall = autoCall;
            Ivr = ivr;
        }

        public int? CallId { get; }

        public string Fio { get; }

        public string[] Phones { get; }

        public bool AutoCall { get; }

        public string Ivr { get; }
    }
}
