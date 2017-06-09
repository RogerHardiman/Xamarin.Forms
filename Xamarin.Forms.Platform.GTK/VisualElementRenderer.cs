﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Gdk;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;
using Container = Gtk.EventBox;
using Control = Gtk.Widget;

namespace Xamarin.Forms.Platform.GTK
{
    public class VisualElementRenderer<TElement, TNativeElement> : Container, IVisualElementRenderer
        where TElement : VisualElement
        where TNativeElement : Control
    {
        private bool _disposed;
        private readonly PropertyChangedEventHandler _propertyChangedHandler;
        private readonly List<EventHandler<VisualElementChangedEventArgs>> _elementChangedHandlers = new List<EventHandler<VisualElementChangedEventArgs>>();
        private VisualElementTracker<TElement, TNativeElement> _tracker;

        protected VisualElementRenderer()
        {
            _propertyChangedHandler = OnElementPropertyChanged;
        }

        protected VisualElementTracker<TElement, TNativeElement> Tracker
        {
            get { return _tracker; }
            set
            {
                if (_tracker == value)
                    return;

                if (_tracker != null)
                {
                    _tracker.Dispose();
                    _tracker.Updated -= OnTrackerUpdated;
                }

                _tracker = value;

                if (_tracker != null)
                {
                    _tracker.Updated += OnTrackerUpdated;
                    UpdateTracker();
                }
            }
        }

        public TNativeElement Control { get; set; }

        public TElement Element { get; set; }

        public Container Container => this;

        public bool Disposed { get { return _disposed; } }

        VisualElement IVisualElementRenderer.Element
        {
            get
            {
                return Element;
            }
        }

        protected IElementController ElementController => Element as IElementController;

        protected virtual bool PreventGestureBubbling { get; set; } = false;

        event EventHandler<VisualElementChangedEventArgs> IVisualElementRenderer.ElementChanged
        {
            add { _elementChangedHandlers.Add(value); }
            remove { _elementChangedHandlers.Remove(value); }
        }

        public event EventHandler<ElementChangedEventArgs<TElement>> ElementChanged;

        void IVisualElementRenderer.SetElement(VisualElement element)
        {
            SetElement((TElement)element);
        }

        public void SetElement(TElement element)
        {
            var oldElement = Element;
            Element = element;

            if (oldElement != null)
            {
                oldElement.PropertyChanged -= _propertyChangedHandler;
            }

            if (element != null)
            {
                element.PropertyChanged += _propertyChangedHandler;

                if (Tracker == null)
                {
                    Tracker = new VisualElementTracker<TElement, TNativeElement>();
                }
            }

            OnElementChanged(new ElementChangedEventArgs<TElement>(oldElement, element));
        }

        public void SetElementSize(Size size)
        {
            Layout.LayoutChildIntoBoundingRegion(Element, new Rectangle(Element.X, Element.Y, size.Width, size.Height));
        }

        protected override bool OnExposeEvent(EventExpose evnt)
        {
            base.OnExposeEvent(evnt);

            Rectangle bounds = Element.Bounds;
            Container.MoveTo(bounds.X, bounds.Y);

            var width = (int)bounds.Width;
            var height = (int)bounds.Height;

            Container.SetSize(width, height);

            for (var i = 0; i < ElementController.LogicalChildren.Count; i++)
            {
                var child = ElementController.LogicalChildren[i] as VisualElement;
                if (child != null)
                {
                    var renderer = Platform.GetRenderer(child);
                    renderer?.Container.SetSize(child.Bounds.Width, child.Bounds.Height);
                    renderer?.Container.MoveTo(child.Bounds.X, child.Bounds.Y);
                }
            }
      
            return true;
        }

        public virtual SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            if (Children.Length == 0)
                return new SizeRequest();

            return Control.GetDesiredSize(widthConstraint, heightConstraint);
        }

        public sealed override void Dispose()
        {
            base.Dispose();

            Dispose(true);
        }

        protected virtual void OnElementChanged(ElementChangedEventArgs<TElement> e)
        {
            var args = new VisualElementChangedEventArgs(e.OldElement, e.NewElement);
            for (var i = 0; i < _elementChangedHandlers.Count; i++)
                _elementChangedHandlers[i](this, args);

            ElementChanged?.Invoke(this, e);
        }

        protected virtual void SetNativeControl(TNativeElement view)
        {
            Control = view;

            UpdateBackgroundColor();
            UpdateIsVisible();
            UpdateSensitive();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
                return;

            _disposed = true;

            Tracker?.Dispose();
            Tracker = null;

            Element = null;

            SetNativeControl(null);
            SetElement(null);
        }

        protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.IsVisibleProperty.PropertyName)
                UpdateIsVisible();
            else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
                UpdateBackgroundColor();
            else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
                UpdateSensitive();
        }

        protected virtual void UpdateBackgroundColor()
        {
            Color backgroundColor = Element.BackgroundColor;

            bool isDefault = backgroundColor.IsDefaultOrTransparent();

            if (!isDefault)
            {
                Container.ModifyBg(StateType.Normal, backgroundColor.ToGtkColor());
            }

            Container.VisibleWindow = !isDefault;
        }

        protected virtual void UpdateNativeControl()
        {
            UpdateSensitive();
        }

        private void UpdateIsVisible()
        {
            Container.Visible = Element.IsVisible;
        }

        private void UpdateSensitive()
        {
            if(Control == null)
            {
                return;
            }

            Control.Sensitive = Element.IsEnabled;
        }

        private void OnTrackerUpdated(object sender, EventArgs e)
        {
            UpdateNativeControl();
        }

        private void UpdateTracker()
        {
            if (_tracker == null)
                return;

            _tracker.PreventGestureBubbling = PreventGestureBubbling;
            _tracker.Control = Control;
            _tracker.Element = Element;
            _tracker.Container = Container;
        }
    }
}