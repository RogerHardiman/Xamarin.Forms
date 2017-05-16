﻿using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
    public class ActivityIndicatorRenderer : ViewRenderer<ActivityIndicator, Controls.ActivityIndicator>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<ActivityIndicator> e)
        {
            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    Controls.ActivityIndicator activityIndicator = new Controls.ActivityIndicator();

                    SetNativeControl(activityIndicator);
                }

                UpdateColor();
                UpdateIsRunning();
            }

            base.OnElementChanged(e);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == ActivityIndicator.ColorProperty.PropertyName)
                UpdateColor();
            else if (e.PropertyName == ActivityIndicator.IsRunningProperty.PropertyName)
                UpdateIsRunning();
        }

        private void UpdateColor()
        {
            var color = Element.Color == Color.Default ? Color.Default.ToGtkColor() : Element.Color.ToGtkColor();

            Control.UpdateColor(color);
        }

        private void UpdateIsRunning()
        {
            if (Element.IsRunning)
                Control.Start();
            else
                Control.Stop();
        }
    }
}
