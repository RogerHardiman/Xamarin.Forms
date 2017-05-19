﻿using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Platform.GTK.Extensions;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using Xamarin.Forms.Platform.GTK.Cells;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
    public class ListViewRenderer : ViewRenderer<ListView, Controls.ListView>
    {
        public const int DefaultRowHeight = 44;

        private bool _disposed;
        private Controls.ListView _listView;
        private IVisualElementRenderer _headerRenderer;
        private IVisualElementRenderer _footerRenderer;
        private List<Gtk.Container> _cells;

        public ListViewRenderer()
        {
            _cells = new List<Gtk.Container>();
        }

        Xamarin.Forms.ListView ListView => Element;

        IListViewController Controller => Element;

        ITemplatedItemsView<Cell> TemplatedItemsView => Element;

        protected override void OnElementChanged(ElementChangedEventArgs<ListView> e)
        {
            if (e.OldElement != null)
            {
                var templatedItems = ((ITemplatedItemsView<Cell>)e.OldElement).TemplatedItems;
                templatedItems.CollectionChanged -= OnCollectionChanged;
            }

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    _listView = new Controls.ListView();
                    _listView.OnSelectedItemChanged += OnSelectedItemChanged;
                    SetNativeControl(_listView);
                }

                var templatedItems = ((ITemplatedItemsView<Cell>)e.NewElement).TemplatedItems;
                templatedItems.CollectionChanged += OnCollectionChanged;

                UpdateItems();
                UpdateGrouping();
                UpdateBackgroundColor();
                UpdateHeader();
                UpdateFooter();
                UpdateRowHeight();
                UpdateHasUnevenRows();
                UpdateSeparatorColor();
                UpdateSeparatorVisibility();
                UpdateIsRefreshing();
                UpdatePullToRefreshEnabled();
            }

            base.OnElementChanged(e);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == ListView.ItemsSourceProperty.PropertyName)
                UpdateItems();
            else if(e.PropertyName == ListView.IsGroupingEnabledProperty.PropertyName)
                UpdateGrouping();
            else if (e.PropertyName.Equals("HeaderElement", StringComparison.InvariantCultureIgnoreCase))
                UpdateHeader();
            else if (e.PropertyName.Equals("FooterElement", StringComparison.InvariantCultureIgnoreCase))
                UpdateFooter();
            else if (e.PropertyName == ListView.RowHeightProperty.PropertyName)
                UpdateRowHeight();
            else if (e.PropertyName == ListView.HasUnevenRowsProperty.PropertyName)
                UpdateHasUnevenRows();
            else if (e.PropertyName == ListView.SeparatorColorProperty.PropertyName)
                UpdateSeparatorColor();
            else if (e.PropertyName == ListView.SeparatorVisibilityProperty.PropertyName)
                UpdateSeparatorVisibility();
            else if (e.PropertyName == ListView.IsRefreshingProperty.PropertyName)
                UpdateIsRefreshing();
            else if (e.PropertyName == ListView.IsPullToRefreshEnabledProperty.PropertyName)
                UpdatePullToRefreshEnabled();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !_disposed)
            {
                _disposed = true;

                _cells = null;

                if (Element != null)
                {
                    var templatedItems = TemplatedItemsView.TemplatedItems;
                    templatedItems.CollectionChanged -= OnCollectionChanged;
                }

                if (_headerRenderer != null)
                {
                    Platform.DisposeModelAndChildrenRenderers(_headerRenderer.Element);
                    _headerRenderer = null;
                }

                if (_listView != null)
                {
                    _listView.OnSelectedItemChanged -= OnSelectedItemChanged;
                }

                if (_footerRenderer != null)
                {
                    Platform.DisposeModelAndChildrenRenderers(_footerRenderer.Element);
                    _footerRenderer = null;
                }
            }
        }

        protected override void UpdateBackgroundColor()
        {
            base.UpdateBackgroundColor();

            if (_listView == null)
            {
                return;
            }

            if (Element.BackgroundColor.IsDefault)
            {
                return;
            }

            var backgroundColor = Element.BackgroundColor.ToGtkColor();

            _listView.SetBackgroundColor(backgroundColor);
        }

        private void UpdateItems()
        {
            _cells.Clear();

            var items = TemplatedItemsView.TemplatedItems;

            if (!items.Any())
            {
                return;
            }

            bool grouping = Element.IsGroupingEnabled;

            if (grouping)
            {
                return;
            }

            foreach (var item in items)
            {
                var cell = GetCell(item);

                _cells.Add(cell);
            }

            _listView.Items = _cells;
        }

        private void UpdateHeader()
        {
            var header = Controller.HeaderElement;
            var headerView = (View)header;

            if (headerView != null)
            {
                _headerRenderer = Platform.CreateRenderer(headerView);
                Platform.SetRenderer(headerView, _headerRenderer);

                _listView.Header = _headerRenderer.Container;
            }
            else
            {
                ClearHeader();
            }
        }

        private void ClearHeader()
        {
            _listView.Header = null;
            if (_headerRenderer == null)
                return;
            Platform.DisposeModelAndChildrenRenderers(_headerRenderer.Element);
            _headerRenderer = null;
        }

        private void UpdateFooter()
        {
            var footer = Controller.FooterElement;
            var footerView = (View)footer;

            if (footerView != null)
            {
                _footerRenderer = Platform.CreateRenderer(footerView);
                Platform.SetRenderer(footerView, _footerRenderer);

                _listView.Footer = _footerRenderer.Container;
            }
            else
            {
                ClearFooter();
            }
        }

        private void ClearFooter()
        {
            _listView.Footer = null;
            if (_footerRenderer == null)
                return;
            Platform.DisposeModelAndChildrenRenderers(_footerRenderer.Element);
            _footerRenderer = null;
        }

        private void UpdateRowHeight()
        {
            var hasUnevenRows = Element.HasUnevenRows;

            if (hasUnevenRows)
            {
                return;
            }

            var rowHeight = Element.RowHeight;

            foreach (var cell in _cells)
            {
                var formsCell = GetXamarinFormsCell(cell);

                if (formsCell != null)
                {
                    var isGroupHeader = formsCell.GetIsGroupHeader<ItemsView<Cell>, Cell>();

                    if (isGroupHeader)
                        cell.HeightRequest = DefaultRowHeight;
                    else
                        cell.HeightRequest = rowHeight > 0 ? rowHeight : DefaultRowHeight;
                }
            }
        }

        private void UpdateHasUnevenRows()
        {
            var hasUnevenRows = Element.HasUnevenRows;

            if (hasUnevenRows)
            {
                foreach (var cell in _cells)
                {
                    var height = GetUnevenRowCellHeight(cell);

                    cell.HeightRequest = height;
                }
            }
            else
            {
                UpdateRowHeight();
            }
        }

        private int GetUnevenRowCellHeight(Gtk.Container cell)
        {
            int height = -1;

            var formsCell = GetXamarinFormsCell(cell);

            if (formsCell != null)
            {
                height = Convert.ToInt32(formsCell.Height);
            }

            return height;
        }

        private Cell GetXamarinFormsCell(Gtk.Container cell)
        {
            try
            {
                var formsCell = cell
                   .GetType()
                   .GetProperty("Cell")
                   .GetValue(cell, null) as Cell;

                return formsCell;
            }
            catch
            {
                return null;
            }
        }

        private void UpdateSeparatorColor()
        {
            if (Element.SeparatorColor.IsDefault)
            {
                return;
            }

            var separatorColor = Element.SeparatorColor.ToGtkColor();

            if (_listView != null)
            {
                _listView.SetSeparatorColor(separatorColor);
            }
        }

        private void UpdateSeparatorVisibility()
        {
            if (_listView == null)
            {
                return;
            }

            var visibility = Element.SeparatorVisibility;

            switch (visibility)
            {
                case SeparatorVisibility.Default:
                    _listView.SetSeparatorVisibility(true);
                    break;
                case SeparatorVisibility.None:
                    _listView.SetSeparatorVisibility(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //TODO: Implement UpdateIsRefreshing
        private void UpdateIsRefreshing()
        {

        }

        //TODO: Implement PullToRefresh
        private void UpdatePullToRefreshEnabled()
        {

        }

        private void UpdateGrouping()
        {
            var templatedItems = TemplatedItemsView.TemplatedItems;

            if (!templatedItems.Any())
            {
                return;
            }

            bool grouping = Element.IsGroupingEnabled;

            if (grouping)
            {
                _cells.Clear();

                int index = 0;
                foreach (var groupItem in templatedItems)
                {
                    var group = templatedItems.GetGroup(index);

                    if (group.Count != 0)
                    {
                        if (group.HeaderContent != null)
                            _cells.Add(GetCell(group.HeaderContent));
                        else
                            _cells.Add(CreateEmptyHeader());

                        foreach (var item in group.ToList())
                        {
                            _cells.Add(GetCell(item as Cell));
                        }
                    }

                    index++;
                }

                _listView.Items = _cells;
            }
        }

        private Cells.TextCell CreateEmptyHeader()
        {
            return new Cells.TextCell(
                string.Empty,
                Color.Black.ToGtkColor(),
                string.Empty,
                Color.Black.ToGtkColor());
        }

        private Gtk.Container GetCell(Cell cell)
        {
            var renderer = 
                (CellRenderer)Registrar.Registered.GetHandler<IRegisterable>(cell.GetType());

            var realCell = renderer.GetCell(cell, null, _listView);

            return realCell;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool grouping = Element.IsGroupingEnabled;

            if (grouping)
                UpdateGrouping();
            else
                UpdateItems();
        }

        private void OnSelectedItemChanged(object sender, Controls.SelectedItemEventArgs args)
        {
            if (_listView != null && _listView.SelectedItem != null)
            {
                ((IElementController)Element).SetValueFromRenderer(
                    ListView.SelectedItemProperty,
                    _listView.SelectedItem);
            }
        }
    }
}