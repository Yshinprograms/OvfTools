using OpenVectorFormat;
using OvfViewer.Services;
using OvfViewer.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OvfViewer.Views
{
    public partial class MainWindow : Window
    {
        private Point _lastMousePosition;
        private bool _isDragging;
        private double _scale = 1.0;
        private Point _offset = new Point(0, 0);
        private WorkPlane? _currentWorkPlane;
        private Point _currentMousePosition;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            
            // Initialize status bar
            UpdateStatusBar();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedLayerSummary) && DataContext is MainViewModel viewModel)
            {
                UpdateCanvas(viewModel);
            }
        }

        private void UpdateCanvas(MainViewModel viewModel)
        {
            if (viewModel.SelectedLayerSummary == null || viewModel.LoadedJob == null)
                return;

            try
            {
                // Get the selected layer index from the summary
                if (int.TryParse(viewModel.SelectedLayerSummary.Split(' ')[1].TrimEnd(':'), out int layerIndex))
                {
                    layerIndex--; // Convert to 0-based index
                    if (layerIndex >= 0 && layerIndex < viewModel.LoadedJob.WorkPlanes.Count)
                    {
                        _currentWorkPlane = viewModel.LoadedJob.WorkPlanes[layerIndex];
                        RenderVectors(_currentWorkPlane);
                        FitToScreen();
                        
                        // Update status message
                        StatusMessage.Content = $"Loaded layer {layerIndex + 1} with {_currentWorkPlane.VectorBlocks?.Count ?? 0} vector blocks";
                    }
                }
            }
            catch (Exception ex)
            {
                // Improved error handling
                StatusMessage.Content = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCanvas: {ex}");
                MessageBox.Show($"Error updating canvas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderVectors(WorkPlane workPlane)
        {
            System.Diagnostics.Debug.WriteLine("Rendering vectors...");
            
            // Clear existing shapes
            VectorCanvas.Children.Clear();
            
            if (workPlane?.VectorBlocks == null || workPlane.VectorBlocks.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No vector blocks to render");
                EmptyStateText.Visibility = Visibility.Visible;
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Found {workPlane.VectorBlocks.Count} vector blocks to render");
            EmptyStateText.Visibility = Visibility.Collapsed;
            
            try
            {
                // Convert vector blocks to WPF shapes
                var shapes = VectorRenderer.ConvertToShapes(workPlane).ToList();
                System.Diagnostics.Debug.WriteLine($"Generated {shapes.Count} shapes");
                
                // Add shapes to canvas
                foreach (var shape in shapes)
                {
                    if (shape != null)
                    {
                        VectorCanvas.Children.Add(shape);
                    }
                }
                
                // Simple identity transform - we're handling positioning in the VectorRenderer
                // This ensures we don't have any transforms interfering with our manual positioning
                VectorCanvas.RenderTransform = null;
                
                // Make sure the canvas is visible
                VectorCanvas.Visibility = Visibility.Visible;
                //Debug.WriteLine("Canvas visibility set to visible");
                
                System.Diagnostics.Debug.WriteLine("Rendering complete");
            }
            catch (Exception ex)
            {
                // Show error message in the canvas
                EmptyStateText.Text = $"Error rendering vectors: {ex.Message}";
                EmptyStateText.Visibility = Visibility.Visible;
                
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error in RenderVectors: {ex}");
                
                // Show error message to user
                MessageBox.Show($"Error rendering vectors: {ex.Message}\n\nDetails: {ex.StackTrace}", 
                    "Rendering Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FitToScreen()
        {
            if (_currentWorkPlane == null || _currentWorkPlane.VectorBlocks == null) return;

            try
            {
                // Get all shapes from the canvas
                var shapes = VectorCanvas.Children.OfType<Shape>().ToList();
                
                // Calculate the bounding box of all shapes
                var boundingBox = VectorRenderer.CalculateBoundingBox(shapes);
                
                // Calculate the scale to fit the bounding box in the canvas
                double scaleX = CanvasGrid.ActualWidth / (boundingBox.Width + 50); // Add padding
                double scaleY = CanvasGrid.ActualHeight / (boundingBox.Height + 50); // Add padding
                
                // Use the smaller scale to ensure everything fits
                _scale = Math.Min(scaleX, scaleY);
                _scale = Math.Max(0.1, Math.Min(_scale, 10.0)); // Limit scale range
                
                // Calculate the offset to center the content
                double centerX = boundingBox.X + boundingBox.Width / 2;
                double centerY = boundingBox.Y + boundingBox.Height / 2;
                
                _offset.X = (CanvasGrid.ActualWidth / 2) - (centerX * _scale);
                _offset.Y = (CanvasGrid.ActualHeight / 2) - (centerY * _scale);
                
                // Apply the transform
                ApplyTransform();
                
                // Update status bar
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                // Fallback to simple fixed scale if there's an error
                _scale = 1.0;
                _offset = new Point(0, 0);
                ApplyTransform();
                
                System.Diagnostics.Debug.WriteLine($"Error in FitToScreen: {ex}");
            }
        }

        private void ApplyTransform()
        {
            VectorRenderer.ApplyZoomAndPan(VectorCanvas, _scale, _offset);
            UpdateStatusBar();
        }

        // Event Handlers for Zoom and Pan
        private void VectorCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            Point mousePos = e.GetPosition(VectorCanvas);
            
            // Calculate zoom center in canvas coordinates
            double canvasX = (mousePos.X - _offset.X) / _scale;
            double canvasY = (mousePos.Y - _offset.Y) / _scale;
            
            // Apply zoom
            _scale *= zoomFactor;
            _scale = Math.Max(0.01, Math.Min(100, _scale)); // Limit zoom range
            
            // Adjust offset to zoom toward mouse position
            _offset.X = mousePos.X - canvasX * _scale;
            _offset.Y = mousePos.Y - canvasY * _scale;
            
            ApplyTransform();
            e.Handled = true;
        }

        private void VectorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(this);
                VectorCanvas.CaptureMouse();
                Mouse.OverrideCursor = Cursors.ScrollAll;
                e.Handled = true;
                
                // Update status message
                StatusMessage.Content = "Dragging canvas...";
            }
        }

        private void VectorCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                VectorCanvas.ReleaseMouseCapture();
                Mouse.OverrideCursor = null;
                e.Handled = true;
                
                // Update status message
                UpdateStatusBar();
            }
        }

        private void VectorCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Update current mouse position
            _currentMousePosition = e.GetPosition(VectorCanvas);
            
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                Vector delta = Point.Subtract(currentPosition, _lastMousePosition);
                
                _offset.X += delta.X;
                _offset.Y += delta.Y;
                
                ApplyTransform();
                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
            
            // Update coordinates in status bar
            UpdateStatusBar();
        }

        private void VectorCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentWorkPlane != null)
            {
                FitToScreen();
            }
        }
        
        private void UpdateStatusBar()
        {
            // Update zoom level display
            ZoomLevel.Content = $"Zoom: {_scale:P0}";
            
            // Update coordinates display
            // Convert screen coordinates to canvas coordinates
            double canvasX = (_currentMousePosition.X - _offset.X) / _scale;
            double canvasY = (_currentMousePosition.Y - _offset.Y) / -_scale; // Invert Y due to the flip transform
            
            CoordinatesDisplay.Content = $"X: {canvasX:F2}, Y: {canvasY:F2}";
        }
        
        // Zoom button event handlers
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            // Zoom in by 20%
            _scale *= 1.2;
            _scale = Math.Min(_scale, 100.0); // Limit maximum zoom
            
            // Center of the canvas as zoom center
            double centerX = CanvasGrid.ActualWidth / 2;
            double centerY = CanvasGrid.ActualHeight / 2;
            
            // Adjust offset to zoom toward center
            double canvasX = (centerX - _offset.X) / (_scale / 1.2);
            double canvasY = (centerY - _offset.Y) / (_scale / 1.2);
            
            _offset.X = centerX - canvasX * _scale;
            _offset.Y = centerY - canvasY * _scale;
            
            ApplyTransform();
            StatusMessage.Content = "Zoomed in";
        }
        
        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            // Zoom out by 20%
            _scale /= 1.2;
            _scale = Math.Max(_scale, 0.01); // Limit minimum zoom
            
            // Center of the canvas as zoom center
            double centerX = CanvasGrid.ActualWidth / 2;
            double centerY = CanvasGrid.ActualHeight / 2;
            
            // Adjust offset to zoom toward center
            double canvasX = (centerX - _offset.X) / (_scale * 1.2);
            double canvasY = (centerY - _offset.Y) / (_scale * 1.2);
            
            _offset.X = centerX - canvasX * _scale;
            _offset.Y = centerY - canvasY * _scale;
            
            ApplyTransform();
            StatusMessage.Content = "Zoomed out";
        }
        
        private void FitToScreenButton_Click(object sender, RoutedEventArgs e)
        {
            FitToScreen();
            StatusMessage.Content = "Fit to screen";
        }
    }
}