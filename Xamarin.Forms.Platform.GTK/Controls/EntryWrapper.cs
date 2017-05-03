﻿using Gdk;
using Gtk;
using Pango;

namespace Xamarin.Forms.Platform.GTK.Controls
{
    public class EntryWrapper : EventBox
    {
        private Gtk.Table _table;
        private Gtk.Entry _entry;
        private Gtk.Label _placeholder;
        private Gtk.EventBox _placeholderContainer;

        public EntryWrapper()
        {
            _table = new Table(1, 1, true);
            _entry = new Gtk.Entry();
            _entry.FocusOutEvent += EntryFocusedOut;
            _placeholder = new Gtk.Label();

            _placeholderContainer = new EventBox();
            _placeholderContainer.Add(_placeholder);
            _placeholderContainer.ButtonPressEvent += PlaceHolderContainerPressed;

            Add(_table);

            _table.Attach(_entry, 0, 1, 0, 1);
            _table.Attach(_placeholderContainer, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 3, 4);
        }

        public Gtk.Entry Entry => _entry;

        public void SetBackgroundColor(Gdk.Color color)
        {
            _entry.ModifyBase(StateType.Normal, color);
            _placeholderContainer.ModifyBg(StateType.Normal, color);
        }

        public void SetPlaceholderText(string text)
        {
            _placeholder.Text = text;
        }

        public void SetPlaceholderTextColor(Gdk.Color color)
        {
            _placeholder.ModifyFg(StateType.Normal, color);
        }

        public void SetAlignment(float aligmentValue)
        {
            _entry.Alignment = aligmentValue;
            _placeholder.SetAlignment(aligmentValue, 0.5f);
        }

        public void SetFont(FontDescription fontDescription)
        {
            _entry.ModifyFont(fontDescription);
            _placeholder.ModifyFont(fontDescription);
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);

            ShowPlaceholderIfNeeded();
        }

        private void PlaceHolderContainerPressed(object o, ButtonPressEventArgs args)
        {
            _entry.GdkWindow?.Raise();
        }

        private void EntryFocusedOut(object o, FocusOutEventArgs args)
        {
            ShowPlaceholderIfNeeded();
        }

        private void ShowPlaceholderIfNeeded()
        {
            if (string.IsNullOrEmpty(_entry.Text) && !string.IsNullOrEmpty(_placeholder.Text))
            {
                _placeholderContainer.GdkWindow?.Raise();
            }
        }
    }
}