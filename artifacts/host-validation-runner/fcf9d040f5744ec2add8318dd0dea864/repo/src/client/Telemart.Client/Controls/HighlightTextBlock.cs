using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using WStyle = System.Windows.Style;

namespace Telemart.Client.Controls
{
    [TemplatePart(Name = NameOfTextBlock, Type = typeof(TextBlock))]
    public class HighlightTextBlock : Control
    {
        private const string NameOfTextBlock = "PART_TextDisplay";

        private TextBlock? _displayTextBlock;

        private WStyle _highlightStyle;

        static HighlightTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HighlightTextBlock), new FrameworkPropertyMetadata(typeof(HighlightTextBlock)));
        }

        #region DependencyProperties

        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.Register(
                nameof(HighlightText),
                typeof(string),
                typeof(HighlightTextBlock),
                new PropertyMetadata(string.Empty, OnHighlightTextPropertyChanged));

        public static readonly DependencyProperty TextProperty =
            TextBlock.TextProperty.AddOwner(typeof(HighlightTextBlock), new PropertyMetadata(string.Empty, OnHighlightTextPropertyChanged));

        public static readonly DependencyProperty IgnoreCaseProperty =
            DependencyProperty.Register(nameof(IgnoreCase), typeof(bool), typeof(HighlightTextBlock));

        public static readonly DependencyProperty HighlightRunStyleProperty =
            DependencyProperty.Register(
                nameof(HighlightRunStyle),
                typeof(WStyle),
                typeof(HighlightTextBlock),
                new PropertyMetadata(CreateDefaultHighlightRunStyle()));

        #endregion

        #region Properties

        public string HighlightText
        {
            get { return (string)GetValue(HighlightTextProperty); }
            set { SetValue(HighlightTextProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public bool IgnoreCase
        {
            get { return (bool)GetValue(IgnoreCaseProperty); }
            set { SetValue(IgnoreCaseProperty, value); }
        }

        public WStyle HighlightRunStyle
        {
            get { return (WStyle)GetValue(HighlightRunStyleProperty); }
            set { SetValue(HighlightRunStyleProperty, value); }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            _displayTextBlock = Template.FindName(NameOfTextBlock, this) as TextBlock;

            base.OnApplyTemplate();

            UpdateHighlightDisplay();
        }

        public override void BeginInit()
        {
            base.BeginInit();

            ControlTemplate template = new ControlTemplate(typeof(HighlightTextBlock));

            FrameworkElementFactory textBlock = new FrameworkElementFactory(typeof(TextBlock), NameOfTextBlock);

            textBlock.SetValue(IsHitTestVisibleProperty, false);

            template.VisualTree = textBlock;

            Template = template;
        }

        private void UpdateHighlightDisplay()
        {
            if (_displayTextBlock is not null)
            {
                _displayTextBlock.Inlines.Clear();

                if (!string.IsNullOrEmpty(HighlightText))
                {
                    int highlightTextLength = HighlightText.Length;

                    if (highlightTextLength == 0)
                    {
                        _displayTextBlock.Text = Text;
                    }
                    else if (!string.IsNullOrEmpty(Text))
                    {
                        for (int i = 0; i < Text.Length; i++)
                        {
                            if (i + highlightTextLength > Text.Length)
                            {
                                _displayTextBlock.Inlines.Add(new Run(Text.Substring(i)));
                                break;
                            }

                            int nextHighlightTextIndex = IgnoreCase ? Text.ToLowerInvariant().IndexOf(HighlightText.ToLowerInvariant(), i) : Text.IndexOf(HighlightText, i);

                            if (nextHighlightTextIndex == -1)
                            {
                                _displayTextBlock.Inlines.Add(new Run(Text.Substring(i)));
                                break;
                            }

                            _displayTextBlock.Inlines.Add(new Run(Text.Substring(i, nextHighlightTextIndex - i)));

                            _displayTextBlock.Inlines.Add(new Run(Text.Substring(nextHighlightTextIndex, HighlightText.Length))
                            {
                                Style = HighlightRunStyle
                            });

                            i = nextHighlightTextIndex + highlightTextLength - 1;
                        }
                    }
                }
                else
                {
                    _displayTextBlock.Text = Text;
                }
            }
        }

        private static WStyle CreateDefaultHighlightRunStyle()
        {
            WStyle style = new WStyle(typeof(Run));

            style.Setters.Add(new Setter(Run.BackgroundProperty, Brushes.Yellow));
            style.Setters.Add(new Setter(Run.ForegroundProperty, Brushes.Black));

            return style;
        }

        private static void OnHighlightTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HighlightTextBlock highlightTextBlock)
            {
                highlightTextBlock.UpdateHighlightDisplay();
            }
        }
    }
}
