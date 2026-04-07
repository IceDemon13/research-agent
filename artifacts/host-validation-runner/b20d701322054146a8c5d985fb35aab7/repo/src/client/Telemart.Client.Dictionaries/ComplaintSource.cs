namespace Telemart.Client.Dictionaries
{
    public class ComplaintSource : DictionaryItem
    {
        private const int RequestId = 1;
        private const int EmailId = 2;
        private const int CallId = 3;
        private const int OurCommentId = 4;
        private const int OtherCommentId = 5;
        private const int ChatId = 6;

        public ComplaintSource(int id, string name)
            : base(id, name, true)
        {
        }

        public static ComplaintSource Request { get; } = new ComplaintSource(RequestId, "Заявление");

        public static ComplaintSource Email { get; } = new ComplaintSource(EmailId, "E-mail");

        public static ComplaintSource Call { get; } = new ComplaintSource(CallId, "Звонок");

        public static ComplaintSource OurComment { get; } = new ComplaintSource(OurCommentId, "Отзыв на сайте");

        public static ComplaintSource OtherComment { get; } = new ComplaintSource(OtherCommentId, "Отзыв на другом ресурсе");

        public static ComplaintSource Chat { get; } = new ComplaintSource(ChatId, "Чат");
    }
}
