using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public sealed class FormatConditionSelector : DataTemplateSelector
    {
        public DataTemplate ChangedTemplate { get; set; }

        public DataTemplate ChangedToEmpty { get; set; }

        public DataTemplate ChangedFromEmpty { get; set; }

        public DataTemplate RedForeground { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate template = null;

            if (item is FormattingRule rule)
            {
                if (rule.ChangeType == PropertyChangeType.None)
                {
                    if (rule.ForegroundColor is not null)
                    {
                        template = rule.ForegroundColor switch
                        {
                            KnownColor.Red => RedForeground,
                            _ => throw new NotSupportedException()
                        };
                    }
                }
                else
                {
                    template = rule.ChangeType switch
                    {
                        PropertyChangeType.Changed => ChangedTemplate,
                        PropertyChangeType.ToEmpty => ChangedToEmpty,
                        PropertyChangeType.FromEmpty => ChangedFromEmpty,
                        _ => throw new NotSupportedException()
                    };
                }
            }

            return template;
        }
    }
}