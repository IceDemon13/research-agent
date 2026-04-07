using System;
using System.Collections.Generic;
using System.Windows;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.ViewComponents.Editors
{
    /// <summary>
    ///     Interaction logic for DatePeriodEdit.xaml
    /// </summary>
    public partial class DatePeriodEdit
    {
        public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
            nameof(From),
            typeof(DateTime?),
            typeof(DatePeriodEdit),
            new PropertyMetadata(default(DateTime?)));

        public static readonly DependencyProperty TillProperty = DependencyProperty.Register(
            nameof(Till),
            typeof(DateTime?),
            typeof(DatePeriodEdit),
            new PropertyMetadata(default(DateTime?)));

        public static readonly DependencyProperty SelectedPeriodProperty = DependencyProperty.Register(
            nameof(SelectedPeriod),
            typeof(DatePeriodEditValue),
            typeof(DatePeriodEdit),
            new PropertyMetadata(default(DatePeriodEditValue)));

        private bool isPeriodChanging;

        public DatePeriodEdit()
        {
            InitializeComponent();
        }

        public DateTime? From
        {
            get => (DateTime?)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        public DateTime? Till
        {
            get => (DateTime?)GetValue(TillProperty);
            set => SetValue(TillProperty, value);
        }

        public DatePeriodEditValue SelectedPeriod
        {
            get => (DatePeriodEditValue)GetValue(SelectedPeriodProperty);
            set => SetValue(SelectedPeriodProperty, value);
        }

        private static IEnumerable<DatePeriodEditValue> GetPeriods()
        {
            yield return DatePeriodEditValue.CurrentMonth;
            yield return DatePeriodEditValue.CurrentWeek;
            yield return DatePeriodEditValue.CurrentYear;
            yield return DatePeriodEditValue.LastMonth;
            yield return DatePeriodEditValue.LastWeek;
            yield return DatePeriodEditValue.LastYear;
        }

        private void PeriodOnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            DatePeriodEditValue period = (DatePeriodEditValue)e.NewValue;

            if (period == DatePeriodEditValue.None)
            {
                return;
            }

            isPeriodChanging = true;

            try
            {
                if (period == DatePeriodEditValue.Empty)
                {
                    From = null;
                    Till = null;
                }
                else
                {
                    (DateTime From, DateTime Till) p = period.GetDateTimePeriod();
                    From = p.From;
                    Till = p.Till;
                }
            }
            finally
            {
                isPeriodChanging = false;
            }

            System.Diagnostics.Debug.WriteLine($"PeriodOnEditValueChanged()->{From:dd.MM.yy}-{Till:dd.MM.yy}");
        }

        private void DateTimeOnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (isPeriodChanging)
            {
                return;
            }

            DatePeriodEditValue period = DatePeriodEditValue.None;

            if (From == null && Till == null)
            {
                period = DatePeriodEditValue.Empty;
            }
            else
            {
                foreach (DatePeriodEditValue x in GetPeriods())
                {
                    (DateTime From, DateTime Till) y = x.GetDateTimePeriod();

                    if (From == y.From && Till == y.Till)
                    {
                        period = x;
                        break;
                    }
                }
            }

            SelectedPeriod = period;

            System.Diagnostics.Debug.WriteLine($"DateTimeOnEditValueChanged() {From:dd.MM.yy}-{Till:dd.MM.yy}");
        }
    }
}