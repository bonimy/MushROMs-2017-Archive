﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MushROMs.Controls
{
    public class TileMapForm : DesignForm
    {
        private TileMapControl _mainTileMapControl;

        public TileMapControl TileMapControl
        {
            get => _mainTileMapControl;
            set
            {
                if (TileMapControl == value)
                    return;

                _mainTileMapControl = value;
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TileMap TileMap => TileMapControl?.TileMap;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Padding MainTileMapPadding
        {
            get;
            private set;
        }

        protected virtual Size MinimumTileSize => new Size(1, 1); protected virtual Size MaximumTileSize => Size.Empty;
        protected virtual void SetTileMapPadding()
        {
            if (TileMapControl != null)
            {
                var form =  WinAPIMethods.GetWindowRectangle(this);
                var child = WinAPIMethods.GetWindowRectangle(TileMapControl);
                var client = WinAPIMethods.DeflateRectangle(child, TileMapControl.BorderPadding);
                MainTileMapPadding = WinAPIMethods.GetPadding(form, client);
            }
            else
                MainTileMapPadding = Padding.Empty;
        }

        protected virtual void SetSizeFromTileMapControl()
        {
            if (TileMapControl.TileMapResizeMode == TileMapResizeMode.ControlResize)
                return;

            if (TileMapControl.TileMapResizeMode == TileMapResizeMode.None)
                TileMapControl.TileMapResizeMode = TileMapResizeMode.ControlResize;

            var client = ClientSize;
            var dbg = WinAPIMethods.InflateSize(client, WindowPadding);
            var window = WinAPIMethods.InflateSize(TileMapControl.ClientSize, MainTileMapPadding);
            window = AdjustSize(window);
            Size = window;

            if (TileMapControl.TileMapResizeMode == TileMapResizeMode.ControlResize)
                TileMapControl.TileMapResizeMode = TileMapResizeMode.None;
        }

        private Rectangle GetTileMapRectangle(Size window)
        {
            return GetTileMapRectangle(new Rectangle(Point.Empty, window));
        }

        private Rectangle GetTileMapRectangle(Rectangle window)
        {
            // Edge case for null rectangles
            if (window.Size == Size.Empty)
                return Rectangle.Empty;

            // Remove the control padding from rect.
            var tilemap = WinAPIMethods.DeflateRectangle(window, MainTileMapPadding);
            var client = WinAPIMethods.GetWindowRectangle(TileMapControl);
            client = WinAPIMethods.DeflateRectangle(client, TileMapControl.BorderPadding);

            // Gets the residual width that is not included in the tilemap size.
            var rWidth = tilemap.Width % TileMap.CellWidth;
            var rHeight = tilemap.Height % TileMap.CellHeight;

            // Get the current dimensions of the window
            var parent = WinAPIMethods.GetWindowRectangle(this);

            // Remove residual area.
            tilemap.Width -= rWidth;
            tilemap.Height -= rHeight;

            // Left or top adjust the client if sizing on those borders.
            if (window.Left != parent.Left && window.Right == parent.Right)
            {
                if (tilemap.Width >= TileMap.CellWidth)
                    tilemap.X += rWidth;
                else
                    tilemap.X = client.X;
            }
            if (window.Top != parent.Top && window.Bottom == parent.Bottom)
            {
                if (tilemap.Height >= TileMap.CellHeight)
                    tilemap.Y += rHeight;
                else
                    tilemap.Y = client.Y;
            }

            // Ensure non-negative values.
            if (tilemap.Width <= 0)
                tilemap.Width = TileMap.CellWidth;
            if (tilemap.Height <= 0)
                tilemap.Height = TileMap.CellHeight;

            return tilemap;
        }

        private Size GetBoundTileSize(Size window)
        {
            var cellW = TileMap.CellWidth;
            var cellH = TileMap.CellHeight;

            // Defines the possible minimum and maximume tile sizes.
            var min = new List<Size>();
            var max = new List<Size>();

            // Min/Max tile size according to the form min/max size.
            min.Add(GetTileMapRectangle(MinimumSize).Size);
            max.Add(GetTileMapRectangle(MaximumSize).Size);

            // Min/Max tile size according to system-defined min/max size.
            min.Add(GetTileMapRectangle(SystemInformation.MinimumWindowSize).Size);
            max.Add(GetTileMapRectangle(SystemInformation.PrimaryMonitorMaximizedWindowSize).Size);

            // Min/Max tile size according to the derived value.
            var tileMin = MinimumTileSize;
            var tileMax = MaximumTileSize;

            // The dimensions need to be on a pixel scale
            tileMin.Width *= cellW;
            tileMin.Height *= cellH;
            tileMax.Width *= cellW;
            tileMax.Height *= cellH;

            min.Add(tileMin);
            max.Add(tileMax);

            // Edge case to prevent zero size
            min.Add(new Size(cellW, cellH));

            // Restrict the lower bound of the tilemap.
            foreach (var size in min)
            {
                window.Width = Math.Max(window.Width, size.Width);
                window.Height = Math.Max(window.Height, size.Height);
            }

            // Restrict upper bounds
            foreach (var size in max)
            {
                if (size.Width > 0)
                    window.Width = Math.Min(window.Width, size.Width);
                if (size.Height > 0)
                    window.Height = Math.Min(window.Height, size.Height);
            }

            return window;
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            //if (TileMapControl.TileMapResizeMode == TileMapResizeMode.None)
            //    TileMapControl.TileMapResizeMode = TileMapResizeMode.FormResize;
            base.OnResizeBegin(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            //if (TileMapControl.TileMapResizeMode == TileMapResizeMode.FormResize)
            //    TileMapControl.TileMapResizeMode = TileMapResizeMode.None;
        }

        protected override Rectangle AdjustSizingRectangle(Rectangle window)
        {
            if (TileMapControl == null || MainTileMapPadding == Padding.Empty)
                return base.AdjustSizingRectangle(window);

            var tilemap = GetTileMapRectangle(window);
            tilemap.Size = GetBoundTileSize(tilemap.Size);

            // Set new tile size
            if (TileMapControl.TileMapResizeMode != TileMapResizeMode.TileMapCellResize)
                TileMap.ViewSize = new Size(tilemap.Width / TileMap.CellWidth,
                    tilemap.Height / TileMap.CellHeight);

            // Adjust window size to bind to tilemap.
            return WinAPIMethods.InflateRectangle(tilemap, MainTileMapPadding);
        }

        protected override Size AdjustSize(Size window)
        {
            if (TileMapControl == null || MainTileMapPadding == Padding.Empty)
                return base.AdjustSize(window);

            var tilemap = GetTileMapRectangle(window).Size;
            tilemap = GetBoundTileSize(tilemap);

            // Set new tile size
            if (TileMapControl.TileMapResizeMode != TileMapResizeMode.TileMapCellResize)
                TileMap.ViewSize = new Size(tilemap.Width / TileMap.CellWidth,
                    tilemap.Height / TileMap.CellHeight);

            // Return inflated window size.
            return WinAPIMethods.InflateSize(tilemap, MainTileMapPadding);
        }
    }
}