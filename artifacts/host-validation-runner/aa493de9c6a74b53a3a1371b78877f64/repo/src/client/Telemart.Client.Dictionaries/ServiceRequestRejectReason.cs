namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRequestRejectReason : DictionaryItem
    {
        public const int EliminatedInPlaceId = 1;
        public const int NotConfirmedId = 2;
        public const int IncorrentReturnConditionsId = 3;
        public const int OtherId = 4;

        public ServiceRequestRejectReason(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceRequestRejectReason EliminatedInPlace { get; } = new ServiceRequestRejectReason(EliminatedInPlaceId, "Дефект устранили по месту");

        public static ServiceRequestRejectReason NotConfirmed { get; } = new ServiceRequestRejectReason(NotConfirmedId, "Дефект не подтвердился");

        public static ServiceRequestRejectReason IncorrentReturnConditions { get; } = new ServiceRequestRejectReason(IncorrentReturnConditionsId, "Отказ, не соблюдены условия возврата или ГО");

        public static ServiceRequestRejectReason Other { get; } = new ServiceRequestRejectReason(OtherId, "Другое");
    }
}
