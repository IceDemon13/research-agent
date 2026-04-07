using System;
using System.Reflection;
using DevExpress.Mvvm;

namespace Telemart.Client.Helpers
{
    public static class ViewModelOpenHelper
    {
        private static readonly Type CurrentType = typeof(ViewModelOpenHelper);

        public static void OpenByMessage(object message, IMessenger messenger, object parentViewModel)
        {
            Type genericType = message.GetType();

            MethodInfo method = CurrentType
                .GetMethod(nameof(SendMessage))!
                .MakeGenericMethod(genericType);

            method.Invoke(
                parentViewModel,
                new[]
                {
                    message,
                    messenger
                });

            messenger.Send(message);
        }

        public static void SendMessage<TMessage>(TMessage message, IMessenger messenger)
        {
            messenger.Send(message);
        }
    }
}