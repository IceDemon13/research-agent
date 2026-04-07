using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace Telemart.Client.ThirdParty.FlowDocuments
{
    internal static class Helpers
    {
        /// <summary>
        /// If you use a bindable flow document element more than once, you may encounter a "Collection was modified" exception.
        /// The error occurs when the binding is updated because of a change to an inherited dependency property. The most common scenario
        /// is when the inherited DataContext changes. It appears that an inherited properly like DataContext is propagated to its descendants.
        /// When the enumeration of descendants gets to a BindableXXX, the dependency properties of that element change according to the new
        /// DataContext, which change the (non-dependency) properties. However, for some reason, changing the flow content invalidates the
        /// enumeration and raises an exception.
        /// To work around this, one can either DataContext="{Binding DataContext, RelativeSource={RelativeSource AncestorType=FrameworkElement}}"
        /// in code. This is clumsy, so every derived type calls this function instead (which performs the same thing).
        /// See http://code.logos.com/blog/2008/01/data_binding_in_a_flowdocument.html.
        /// </summary>
        /// <param name="element">Framework element.</param>
        public static void FixupDataContext(FrameworkContentElement element)
        {
            Binding binding = new Binding(FrameworkContentElement.DataContextProperty.Name)
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FrameworkElement), 1)
            };

            // another approach (if this one has problems) is to bind to an ancestor by ElementName
            element.SetBinding(FrameworkContentElement.DataContextProperty, binding);
        }

        public static void UnFixupDataContext(DependencyObject dp)
        {
            while (InternalUnFixupDataContext(dp))
            {
            }
        }

        /// <summary>
        /// Convert "data" to a flow document block object. If data is already a block, the return value is data recast.
        /// </summary>
        /// <param name="dataContext">only used when bindable content needs to be created.</param>
        /// <param name="data">Data.</param>
        /// <returns>Block.</returns>
        public static Block ConvertToBlock(object dataContext, object data)
        {
            Block block = data as Block;

            if (block != null)
            {
                return block;
            }

            Inline inline = data as Inline;

            if (inline != null)
            {
                return new Paragraph(inline);
            }

            BindingBase bindingBase = data as BindingBase;

            if (bindingBase != null)
            {
                BindableRun run = new BindableRun();

                BindingBase binding = dataContext as BindingBase;

                if (binding != null)
                {
                    run.SetBinding(FrameworkContentElement.DataContextProperty, binding);
                }
                else
                {
                    run.DataContext = dataContext;
                }

                run.SetBinding(BindableRun.BoundTextProperty, bindingBase);

                return new Paragraph(run);
            }
            else
            {
                Run run = new Run { Text = data?.ToString() ?? string.Empty };

                return new Paragraph(run);
            }
        }

        public static FrameworkContentElement LoadDataTemplate(DataTemplate dataTemplate)
        {
            object content = dataTemplate.LoadContent();

            Fragment fragment = content as Fragment;

            if (fragment != null)
            {
                return fragment.Content;
            }

            TextBlock textBlock = content as TextBlock;

            if (textBlock != null)
            {
                InlineCollection inlines = textBlock.Inlines;

                if (inlines.Count == 1)
                {
                    return inlines.FirstInline;
                }

                Paragraph paragraph = new Paragraph();

                // we can't use an enumerator, since adding an inline removes it from its collection
                while (inlines.FirstInline != null)
                {
                    paragraph.Inlines.Add(inlines.FirstInline);
                }

                return paragraph;
            }

            throw new InvalidOperationException("Data template needs to contain a <Fragment> or <TextBlock>");
        }

        private static bool InternalUnFixupDataContext(DependencyObject dp)
        {
            // only consider those elements for which we've called FixupDataContext(): they all belong to this namespace
            if (dp is FrameworkContentElement && dp.GetType().Namespace == typeof(Helpers).Namespace)
            {
                Binding binding = BindingOperations.GetBinding(dp, FrameworkContentElement.DataContextProperty);
                if (binding?.Path != null
                    && binding.Path.Path == FrameworkContentElement.DataContextProperty.Name
                    && binding.RelativeSource != null
                    && binding.RelativeSource.Mode == RelativeSourceMode.FindAncestor
                    && binding.RelativeSource.AncestorType == typeof(FrameworkElement)
                    && binding.RelativeSource.AncestorLevel == 1)
                {
                    BindingOperations.ClearBinding(dp, FrameworkContentElement.DataContextProperty);
                    return true;
                }
            }

            // as soon as we have disconnected a binding, return. Don't continue the enumeration, since the collection may have changed
            foreach (object child in LogicalTreeHelper.GetChildren(dp))
            {
                DependencyObject o = child as DependencyObject;

                if (o != null)
                {
                    if (InternalUnFixupDataContext(o))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}