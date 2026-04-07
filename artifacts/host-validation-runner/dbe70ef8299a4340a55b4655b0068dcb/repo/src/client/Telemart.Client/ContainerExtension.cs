using System;
using System.Windows.Markup;

namespace Telemart.Client
{
    [ContentProperty("TypeArguments")]
    public sealed class ContainerExtension : MarkupExtension
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public Type Type { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return ServiceProvider.GetService(Type);
        }
    }
}