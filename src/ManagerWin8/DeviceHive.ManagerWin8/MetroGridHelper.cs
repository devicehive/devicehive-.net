using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace MC.MetroGridHelper
{
    /// <summary>
    /// A utility class that overlays a designer-friendly grid on top of the
    /// application frame, for use similar to the performance counters in
    /// App.xaml.cs. The color and opacity are configurable. The grid contains
    /// a number of squares that are 24x24, offset with 12px gutters, and all
    /// 24px away from the edge of the device.
    /// </summary>
    public static class MetroGridHelper
    {
        private static bool _visible;
        private static double _opacity = 0.15;
        private static Color _color = Colors.Red;
        private static List<Shape> _shapes;
        private static Grid _grid;
        private static bool _eventsAttached;
        private static double _width;
        private static double _height;

        /// <summary>
        /// Gets or sets a value indicating whether the designer grid is
        /// visible on top of the application's frame.
        /// </summary>
        public static bool IsVisible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                UpdateGrid();
            }
        }

        /// <summary>
        /// Gets or sets the color to use for the grid's squares.
        /// </summary>
        public static Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                UpdateGrid();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the opacity for the grid's squares.
        /// </summary>
        public static double Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
                UpdateGrid();
            }
        }

        /// <summary>
        /// Updates the grid (if it already has been created) or initializes it
        /// otherwise.
        /// </summary>
        private static void UpdateGrid()
        {
            if (_shapes != null)
            {
                var brush = new SolidColorBrush(_color);
                foreach (Shape square in _shapes)
                {
                    square.Fill = brush;
                }
                if (_grid != null)
                {
                    _grid.Visibility = _visible ? Visibility.Visible : Visibility.Collapsed;
                    _grid.Opacity = _opacity;
                }
            }
            else
            {
                BuildGrid();
            }
        }

        /// <summary>
        /// Builds the grid.
        /// </summary>
        private static async void BuildGrid()
        {
            _shapes = new List<Shape>();

            var frame = Window.Current.Content as Frame;
            if (frame == null || VisualTreeHelper.GetChildrenCount(frame) == 0)
            {
                await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, BuildGrid);
                return;
            }

            DependencyObject child = VisualTreeHelper.GetChild(frame, 0);
            var childAsBorder = child as Border;
            var childAsGrid = child as Grid;
            if (childAsBorder != null)
            {
                // Not a pretty way to control the root visual, but I did not
                // want to implement using a popup.
                UIElement content = childAsBorder.Child;
                if (content == null)
                {
                    await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, BuildGrid);
                    return;
                }
                childAsBorder.Child = null;
                await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                                         () =>
                                                             {
                                                                 var newGrid = new Grid();
                                                                 childAsBorder.Child = newGrid;
                                                                 newGrid.Children.Add(content);
                                                                 PrepareGrid(newGrid);
                                                                 AttachEvents();
                                                             });
            }
            else if (childAsGrid != null)
            {
                PrepareGrid(childAsGrid);
                AttachEvents();
            }
            else
            {
                Debug.WriteLine("Dear developer:");
                Debug.WriteLine("Unfortunately the design overlay feature requires that the root frame visual");
                Debug.WriteLine("be a Border or a Grid. So the overlay grid just isn't going to happen.");
            }
        }

        private static void AttachEvents()
        {
            if (_eventsAttached)
                return;
            Window.Current.SizeChanged += (s, e) => BuildGridForCurrentView();
            DisplayProperties.OrientationChanged += s => BuildGridForCurrentView(); 
            _eventsAttached = true;
        }

        public static void CreateGrid()
        {
            IsVisible = true;
        }

        /// <summary>
        /// Does the actual work of preparing the grid once the parent frame is
        /// in the visual tree and we have a Grid instance to work with for
        /// placing the chilren.
        /// </summary>
        /// <param name="parent">The parent grid to insert the sub-grid into.</param>
        private static void PrepareGrid(Grid parent)
        {
            if (_grid == null)
            {
                _grid = new Grid
                            {
                                IsHitTestVisible = false,
                                Visibility = _visible ? Visibility.Visible : Visibility.Collapsed,
                                Opacity = _opacity,
                                CacheMode = new BitmapCache()
                            };
                parent.Children.Add(_grid);
            }
            BuildGridForCurrentView();
        }

        public static int GetMarginForCurrentView()
        {
            bool isSnapped = ApplicationView.Value == ApplicationViewState.Snapped;
            bool isPortrait = DisplayProperties.CurrentOrientation == DisplayOrientations.Portrait ||
                              DisplayProperties.CurrentOrientation == DisplayOrientations.PortraitFlipped;
            if (isSnapped)
                return 20;
            if (isPortrait)
                return 100;
            return 120;
        }

        public static void BuildGridForCurrentView()
        {
            // Places the grid into the visual tree. It is never removed once
            // being added.
            var frame = Window.Current.Content as Frame;
            if (frame == null)
                return;

            _width=Math.Max(_width, frame.ActualWidth);
            _height=Math.Max(_height, frame.ActualHeight);
            _grid.Children.Clear();
            _shapes.Clear();
            IEnumerable<Shape> shapes = GetGridShapesForMargin(_width, _height, GetMarginForCurrentView());
            foreach (Shape shape in shapes)
            {
                _grid.Children.Add(shape);
                _shapes.Add(shape);
            }
        }

        public static IEnumerable<Shape> GetGridShapesForMargin(double width, double height, int margin)
        {
            // To support both orientations, unfortunately more visuals need to
            // be used. An alternate implementation would be to react to the
            // orientation change event and re-draw/remove squares.
            var brush = new SolidColorBrush(_color);
            double max = Math.Max(width, height);

            const double strokeWidth = 2.0;

            var horizontalLine = new Line
                                     {
                                         IsHitTestVisible = false,
                                         Stroke = brush,
                                         X1 = 0,
                                         X2 = max,
                                         Y1 = 100 + (strokeWidth/2),
                                         Y2 = 100 + (strokeWidth/2),
                                         StrokeThickness = strokeWidth,
                                     };
            yield return horizontalLine;
            var horizontalLine2 = new Line
                                      {
                                          IsHitTestVisible = false,
                                          Stroke = brush,
                                          X1 = 0,
                                          X2 = max,
                                          Y1 = 140 + (strokeWidth/2),
                                          Y2 = 140 + (strokeWidth/2),
                                          StrokeThickness = strokeWidth,
                                      };
            yield return horizontalLine2;

            var verticalLine = new Line
                                   {
                                       IsHitTestVisible = false,
                                       Stroke = brush,
                                       X1 = margin - (strokeWidth / 2),
                                       X2 = margin - (strokeWidth / 2),
                                       Y1 = 0,
                                       Y2 = max,
                                       StrokeThickness = strokeWidth,
                                   };
            yield return verticalLine;

            var horizontalBottomLine = new Line
                                           {
                                               IsHitTestVisible = false,
                                               Stroke = brush,
                                               X1 = 0,
                                               X2 = max,
                                               Y1 = height - 130 + (strokeWidth/2),
                                               Y2 = height - 130 + (strokeWidth/2),
                                               StrokeThickness = strokeWidth,
                                           };
            _shapes.Add(horizontalBottomLine);
            var horizontalBottomLine2 = new Line
                                            {
                                                IsHitTestVisible = false,
                                                Stroke = brush,
                                                X1 = 0,
                                                X2 = max,
                                                Y1 = height - 50 + (strokeWidth/2),
                                                Y2 = height - 50 + (strokeWidth/2),
                                                StrokeThickness = strokeWidth,
                                            };
            _shapes.Add(horizontalBottomLine2);
            yield return horizontalBottomLine2;

            const int tileHeight = 20;

            for (int x = margin; x < /*width*/ max; x += (tileHeight*2))
            {
                for (int y = 140; y < /*height*/ max; y += (tileHeight*2))
                {
                    var rect = new Rectangle
                                   {
                                       Width = tileHeight,
                                       Height = tileHeight,
                                       VerticalAlignment = VerticalAlignment.Top,
                                       HorizontalAlignment = HorizontalAlignment.Left,
                                       Margin = new Thickness(x, y, 0, 0),
                                       IsHitTestVisible = false,
                                       Fill = brush,
                                   };
                    yield return rect;
                }
            }
        }
    }
}