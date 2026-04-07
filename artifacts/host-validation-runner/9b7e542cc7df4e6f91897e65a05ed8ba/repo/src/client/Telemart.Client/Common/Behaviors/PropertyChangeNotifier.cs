using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace Telemart.Client.Common.Behaviors
{
    public sealed class PropertyChangeNotifier :
        DependencyObject,
        IDisposable
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(object),
            typeof(PropertyChangeNotifier),
            new FrameworkPropertyMetadata(null, OnPropertyChanged));

        private readonly WeakReference propertySource;

        public PropertyChangeNotifier(DependencyObject propertySource, string path)
            : this(propertySource, new PropertyPath(path))
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
            : this(propertySource, new PropertyPath(property))
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property)
        {
            if (propertySource == null)
            {
                throw new ArgumentNullException(nameof(propertySource));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            this.propertySource = new WeakReference(propertySource);

            Binding binding = new Binding { Path = property, Mode = BindingMode.OneWay, Source = propertySource };

            BindingOperations.SetBinding(this, ValueProperty, binding);
        }

        public event EventHandler ValueChanged;

        public DependencyObject PropertySource
        {
            get
            {
                try
                {
                    // note, it is possible that accessing the target property will result in an exception so i've wrapped this check in a try catch
                    return propertySource.IsAlive
                        ? propertySource.Target as DependencyObject
                        : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        [Description("Returns / sets the value of the property")]
        [Category("Behavior")]
        [Bindable(true)]
        public object Value
        {
            get
            {
                return GetValue(ValueProperty);
            }

            set
            {
                SetValue(ValueProperty, value);
            }
        }

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ValueProperty);
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyChangeNotifier notifier = (PropertyChangeNotifier)d;
            notifier.ValueChanged?.Invoke(notifier, EventArgs.Empty);
        }
    }
}