using System.Windows;
using System.Windows.Controls;

namespace FModel.Views.Resources.Controls
{
    public class CustomScrollViewer : ScrollViewer
    {
        /// <summary>
        /// VerticalOffset attached property
        /// </summary>
        public new static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double),
                typeof(CustomScrollViewer), new FrameworkPropertyMetadata(double.NaN,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVerticalOffsetPropertyChanged));

        /// <summary>
        /// Just a flag that the binding has been applied.
        /// </summary>
        private static readonly DependencyProperty _verticalScrollBindingProperty =
            DependencyProperty.RegisterAttached("_verticalScrollBinding", typeof(bool?), typeof(CustomScrollViewer));

        public static double GetVerticalOffset(DependencyObject depObj)
        {
            return (double) depObj.GetValue(VerticalOffsetProperty);
        }

        public static void SetVerticalOffset(DependencyObject depObj, double value)
        {
            depObj.SetValue(VerticalOffsetProperty, value);
        }

        private static void OnVerticalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var v = (double) e.NewValue;
            if (d is not ScrollViewer scrollViewer || double.IsNaN(v))
                return;

            BindVerticalOffset(scrollViewer);
            scrollViewer.ScrollToVerticalOffset(v);
        }

        private static void BindVerticalOffset(ScrollViewer scrollViewer)
        {
            if (scrollViewer.GetValue(_verticalScrollBindingProperty) != null)
                return;

            scrollViewer.SetValue(_verticalScrollBindingProperty, true);
            scrollViewer.ScrollChanged += (s, se) =>
            {
                if (se.VerticalChange == 0)
                    return;
                SetVerticalOffset(scrollViewer, se.VerticalOffset);
            };
        }
    }
}