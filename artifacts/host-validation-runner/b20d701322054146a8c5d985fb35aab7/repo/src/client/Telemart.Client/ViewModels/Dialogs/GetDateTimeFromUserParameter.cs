using System;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class GetDateTimeFromUserParameter
    {
        public GetDateTimeFromUserParameter(
            string contentCaption,
            string title,
            bool withTime = false,
            DateTime? defaultDateTime = null,
            DateTime? minDateTime = null,
            DateTime? maxDateTime = null,
            bool nowIsMinTime = false)
        {
            ContentCaption = contentCaption;
            Title = title;
            WithTime = withTime;
            DefaultDateTime = defaultDateTime;
            MinDateTime = minDateTime;
            MaxDateTime = maxDateTime;
            NowIsMinTime = nowIsMinTime;
        }

        public string ContentCaption { get; }

        public string Title { get; }

        public bool WithTime { get; }

        public bool NowIsMinTime { get; }

        public DateTime? DefaultDateTime { get; }

        public DateTime? MinDateTime { get; }

        public DateTime? MaxDateTime { get; }
    }
}