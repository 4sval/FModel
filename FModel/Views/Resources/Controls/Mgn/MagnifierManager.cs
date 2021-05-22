using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace FModel.Views.Resources.Controls
{
    public class MagnifierManager : DependencyObject
    {
        private MagnifierAdorner _adorner;
        private UIElement _element;

        public static readonly DependencyProperty CurrentProperty =
            DependencyProperty.RegisterAttached("Magnifier", typeof(Magnifier), typeof(UIElement), new FrameworkPropertyMetadata(null, OnMagnifierChanged));

        public static void SetMagnifier(UIElement element, Magnifier value)
        {
            element.SetValue(CurrentProperty, value);
        }

        public static Magnifier GetMagnifier(UIElement element)
        {
            return (Magnifier) element.GetValue(CurrentProperty);
        }

        private static void OnMagnifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement target)
                throw new ArgumentException("Magnifier can only be attached to a UIElement.");

            new MagnifierManager().AttachToMagnifier(target, e.NewValue as Magnifier);
        }

        private void ElementOnMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            if (GetMagnifier(_element) is {IsFrozen: true})
                return;

            HideAdorner();
        }

        private void ElementOnMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            ShowAdorner();
        }

        private void ElementOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (GetMagnifier(_element) is not {IsUsingZoomOnMouseWheel: true} magnifier) return;

            switch (e.Delta)
            {
                case < 0:
                {
                    var newValue = magnifier.ZoomFactor + magnifier.ZoomFactorOnMouseWheel;
                    magnifier.SetCurrentValue(Magnifier.ZoomFactorProperty, newValue);
                    break;
                }
                case > 0:
                {
                    var newValue = magnifier.ZoomFactor >= magnifier.ZoomFactorOnMouseWheel ? magnifier.ZoomFactor - magnifier.ZoomFactorOnMouseWheel : 0d;
                    magnifier.SetCurrentValue(Magnifier.ZoomFactorProperty, newValue);
                    break;
                }
            }

            _adorner.UpdateViewBox();
        }

        private void AttachToMagnifier(UIElement element, Magnifier magnifier)
        {
            _element = element;
            _element.MouseLeftButtonDown += ElementOnMouseLeftButtonDown;
            _element.MouseLeftButtonUp += ElementOnMouseLeftButtonUp;
            _element.MouseWheel += ElementOnMouseWheel;

            magnifier.Target = _element;

            _adorner = new MagnifierAdorner(_element, magnifier);
        }

        private void ShowAdorner()
        {
            VerifyAdornerLayer();
            _adorner.Visibility = Visibility.Visible;
        }

        private void VerifyAdornerLayer()
        {
            if (_adorner.Parent != null) return;
            var layer = AdornerLayer.GetAdornerLayer(_element);
            layer?.Add(_adorner);
        }

        private void HideAdorner()
        {
            if (_adorner.Visibility == Visibility.Visible)
            {
                _adorner.Visibility = Visibility.Collapsed;
            }
        }
    }
}