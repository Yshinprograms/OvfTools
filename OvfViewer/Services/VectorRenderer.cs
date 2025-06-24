using OpenVectorFormat;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OvfViewer.Services
{

    public static class VectorRenderer
    {
        public static IEnumerable<Shape> ConvertToShapes(WorkPlane workPlane)
        {
            int scalefactor = 20; // Scale factor for visibility

            var shapes = new List<Shape>();
            if (workPlane?.VectorBlocks == null) return shapes;

            // Debug: Log total number of vector blocks
            Debug.WriteLine($"Processing {workPlane.VectorBlocks.Count} vector blocks");

            // Just draw simple lines between points
            foreach (var block in workPlane.VectorBlocks) {
                Debug.WriteLine($"Processing block type: {block.VectorDataCase}");

                if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence) {
                    var points = block.LineSequence.Points;
                    if (points == null) {
                        Debug.WriteLine("Points collection is null");
                        continue;
                    }

                    Debug.WriteLine($"Found {points.Count} points in LineSequence");

                    if (points.Count < 2) {
                        Debug.WriteLine("Not enough points to form a line");
                        continue;
                    }

                    // Draw lines between consecutive points
                    for (int i = 0; i <= points.Count - 4; i += 2) {
                        try {
                            Debug.WriteLine($"Line from ({points[i]}, {points[i + 1]}) to ({points[i + 2]}, {points[i + 3]})");

                            // Create a very visible line
                            var line = new Line
                            {
                                X1 = points[i] * scalefactor + 250,  // Scale up and center
                                Y1 = points[i + 1] * scalefactor + 250,  // Scale up and center
                                X2 = points[i + 2] * scalefactor + 250,  // Scale up and center
                                Y2 = points[i + 3] * scalefactor + 250,  // Scale up and center
                                Stroke = Brushes.Red,
                                StrokeThickness = 3.0,  // Even thicker lines
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            
                            // Add a white outline for better visibility
                            var outline = new Line
                            {
                                X1 = line.X1,
                                Y1 = line.Y1,
                                X2 = line.X2,
                                Y2 = line.Y2,
                                Stroke = Brushes.White,
                                StrokeThickness = 5.0,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            shapes.Add(outline);
                            shapes.Add(line);
                        } catch (Exception ex) {
                            Debug.WriteLine($"Error creating line: {ex.Message}");
                        }
                    }
                }
                else if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches) {
                    // Handle hatches
                    var hatches = block.Hatches;
                    if (hatches == null) {
                        Debug.WriteLine("Hatches collection is null");
                        continue;
                    }

                    // The Hatches message contains a 'points' array where each pair of consecutive points forms a line
                    // Format: [x1, y1, x2, y2, x3, y3, ...] where (x1,y1)-(x2,y2) is first line, (x3,y3)-(x4,y4) is second line, etc.
                    var points = hatches.Points;
                    if (points == null || points.Count < 2) {
                        Debug.WriteLine("Hatches points collection is null or has insufficient points");
                        continue;
                    }

                    Debug.WriteLine($"Found {points.Count / 4} hatch lines");

                    // Process points in pairs to create lines
                    for (int i = 0; i <= points.Count - 4; i += 4) {
                        try {
                            // Create a line for each hatch
                            var line = new Line
                            {
                                X1 = points[i] * scalefactor + 250,
                                Y1 = points[i + 1] * scalefactor + 250,
                                X2 = points[i + 2] * scalefactor + 250,
                                Y2 = points[i + 3] * scalefactor + 250,
                                Stroke = Brushes.Blue, // Different color for hatches
                                StrokeThickness = 2.0,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            
                            // Add a white outline for better visibility
                            var outline = new Line
                            {
                                X1 = line.X1,
                                Y1 = line.Y1,
                                X2 = line.X2,
                                Y2 = line.Y2,
                                Stroke = Brushes.White,
                                StrokeThickness = 4.0,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            shapes.Add(outline);
                            shapes.Add(line);
                        } catch (Exception ex) {
                            Debug.WriteLine($"Error creating hatch line: {ex.Message}");
                        }
                    }
                }
            }

            return shapes;
        }

        // New method that returns both shapes and labels for vector blocks
        public static (List<Shape> Shapes, List<UIElement> Labels) ConvertToShapesWithLabels(WorkPlane workPlane)
        {
            int scalefactor = 10; // Scale factor for visibility

            var shapes = new List<Shape>();
            var labels = new List<UIElement>();
            if (workPlane?.VectorBlocks == null) return (shapes, labels);

            // Debug: Log total number of vector blocks
            Debug.WriteLine($"Processing {workPlane.VectorBlocks.Count} vector blocks");

            // Just draw simple lines between points
            for (int blockIndex = 0; blockIndex < workPlane.VectorBlocks.Count; blockIndex++) 
            {
                var block = workPlane.VectorBlocks[blockIndex];
                Debug.WriteLine($"Processing block type: {block.VectorDataCase}");

                // Calculate the center point for the label placement
                double centerX = 0;
                double centerY = 0;
                bool hasValidGeometry = false;
                
                if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence) 
                {
                    var points = block.LineSequence.Points;
                    if (points == null) 
                    {
                        Debug.WriteLine("Points collection is null");
                        continue;
                    }

                    Debug.WriteLine($"Found {points.Count} points in LineSequence");

                    if (points.Count < 2) 
                    {
                        Debug.WriteLine("Not enough points to form a line");
                        continue;
                    }
                    
                    // Draw lines between consecutive points
                    for (int i = 0; i <= points.Count - 4; i += 2) 
                    {
                        try 
                        {
                            Debug.WriteLine($"Line from ({points[i]}, {points[i + 1]}) to ({points[i + 2]}, {points[i + 3]})");

                            // Create a very visible line
                            var line = new Line
                            {
                                X1 = points[i] * scalefactor + 250,  // Scale up and center
                                Y1 = points[i + 1] * scalefactor + 250,  // Scale up and center
                                X2 = points[i + 2] * scalefactor + 250,  // Scale up and center
                                Y2 = points[i + 3] * scalefactor + 250,  // Scale up and center
                                Stroke = Brushes.Red,
                                StrokeThickness = 3.0,  // Even thicker lines
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            
                            // Add a white outline for better visibility
                            var outline = new Line
                            {
                                X1 = line.X1,
                                Y1 = line.Y1,
                                X2 = line.X2,
                                Y2 = line.Y2,
                                Stroke = Brushes.White,
                                StrokeThickness = 5.0,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            shapes.Add(outline);
                            shapes.Add(line);
                            
                            // For the first line segment, calculate center point for label placement
                            if (i == 0)
                            {
                                centerX = (line.X1 + line.X2) / 2;
                                centerY = (line.Y1 + line.Y2) / 2;
                                hasValidGeometry = true;
                            }
                        } 
                        catch (Exception ex) 
                        {
                            Debug.WriteLine($"Error creating line: {ex.Message}");
                        }
                    }
                }
                else if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches) 
                {
                    // Handle hatches
                    var hatches = block.Hatches;
                    if (hatches == null) 
                    {
                        Debug.WriteLine("Hatches collection is null");
                        continue;
                    }
                    
                    // The Hatches message contains a 'points' array where each pair of consecutive points forms a line
                    // Format: [x1, y1, x2, y2, x3, y3, ...] where (x1,y1)-(x2,y2) is first line, (x3,y3)-(x4,y4) is second line, etc.
                    var points = hatches.Points;
                    if (points == null || points.Count < 4) 
                    {
                        Debug.WriteLine("Hatches points collection is null or has insufficient points");
                        continue;
                    }

                    Debug.WriteLine($"Found {points.Count / 4} hatch lines");
                    
                    bool isFirstLine = true;
                    // Process points in pairs to create lines
                    for (int i = 0; i <= points.Count - 4; i += 4) 
                    {
                        try 
                        {
                            // Create a line for each hatch
                            var line = new Line
                            {
                                X1 = points[i] * scalefactor + 250,
                                Y1 = points[i + 1] * scalefactor + 250,
                                X2 = points[i + 2] * scalefactor + 250,
                                Y2 = points[i + 3] * scalefactor + 250,
                                Stroke = Brushes.Blue, // Different color for hatches
                                StrokeThickness = 2.0,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            
                            // Add a white outline for better visibility
                            var outline = new Line
                            {
                                X1 = line.X1,
                                Y1 = line.Y1,
                                X2 = line.X2,
                                Y2 = line.Y2,
                                Stroke = Brushes.White,
                                StrokeThickness = 4.0,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round
                            };
                            shapes.Add(outline);
                            shapes.Add(line);
                            
                            // For the first hatch line, calculate center point for label placement
                            if (isFirstLine)
                            {
                                centerX = (line.X1 + line.X2) / 2;
                                centerY = (line.Y1 + line.Y2) / 2;
                                hasValidGeometry = true;
                                isFirstLine = false;
                            }
                        } 
                        catch (Exception ex) 
                        {
                            Debug.WriteLine($"Error creating hatch line: {ex.Message}");
                        }
                    }
                }
                
                // Create a label for this vector block if it has valid geometry
                if (hasValidGeometry)
                {
                    // Create a border for better visibility with more distinctive styling
                    var border = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), // Semi-transparent white
                        BorderBrush = block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence ? Brushes.Red : Brushes.Blue,
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(4, 2, 4, 2),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            ShadowDepth = 2,
                            Opacity = 0.5,
                            BlurRadius = 4
                        }
                    };
                    
                    // Create the label text
                    var label = new TextBlock
                    {
                        Text = blockIndex.ToString(),
                        Foreground = block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence ? Brushes.Red : Brushes.Blue,
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    // Add the label to the border
                    border.Child = label;
                    
                    // Position the border at the center of the first line segment
                    Canvas.SetLeft(border, centerX - 15); // Increased offset to better center the label
                    Canvas.SetTop(border, centerY - 15);  // Increased offset to better center the label
                    
                    // Set z-index to ensure labels are on top of all other elements
                    Canvas.SetZIndex(border, 1000);
                    
                    labels.Add(border);
                    
                    // Debug output to verify label creation
                    Debug.WriteLine($"Created label for block {blockIndex} at position ({centerX}, {centerY})");
                }
            }

            return (shapes, labels);
        }

        public static void ApplyZoomAndPan(Canvas canvas, double scale, Point offset)
        {
            if (canvas == null) return;
            
            // Create a transform group to combine multiple transforms
            var transformGroup = new TransformGroup();
            
            // Add scale transform
            transformGroup.Children.Add(new ScaleTransform(scale, -scale)); // Maintain Y-axis flip while scaling
            
            // Add translate transform for panning
            transformGroup.Children.Add(new TranslateTransform(offset.X, offset.Y));
            
            // Apply the transform group
            canvas.RenderTransform = transformGroup;
            
            // Set a background to make the canvas visible
            canvas.Background = Brushes.LightGray;
            
            // Ensure the canvas is visible and properly sized
            canvas.Visibility = Visibility.Visible;
            canvas.ClipToBounds = true;
            
            // Ensure the canvas fills its container
            canvas.HorizontalAlignment = HorizontalAlignment.Stretch;
            canvas.VerticalAlignment = VerticalAlignment.Stretch;
            
            // Make sure the canvas has a parent panel
            if (canvas.Parent is Panel parentPanel)
            {
                // Set the parent panel background to ensure consistent appearance
                parentPanel.Background = Brushes.White;
                
                // Ensure the parent panel clips its children
                if (parentPanel is Grid grid)
                {
                    grid.ClipToBounds = true;
                }
            }
            
            // If the canvas is inside a ScrollViewer, disable it to prevent conflicts with our custom panning
            if (canvas.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
        }

        public static void ClearCanvas(Canvas canvas)
        {
            canvas?.Children.Clear();
        }

        public static void AddShapesToCanvas(Canvas canvas, IEnumerable<Shape> shapes)
        {
            if (canvas == null || shapes == null) return;
            
            foreach (var shape in shapes)
            {
                if (shape != null)
                {
                    canvas.Children.Add(shape);
                }
            }
        }
        
        // New method to add both shapes and labels to canvas
        public static void AddElementsToCanvas(Canvas canvas, IEnumerable<Shape> shapes, IEnumerable<UIElement> labels)
        {
            if (canvas == null) return;
            
            // Add shapes first
            if (shapes != null)
            {
                foreach (var shape in shapes)
                {
                    if (shape != null)
                    {
                        canvas.Children.Add(shape);
                    }
                }
            }
            
            // Add labels on top
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    if (label != null)
                    {
                        canvas.Children.Add(label);
                    }
                }
            }
        }
        
        // Calculate the bounding box of all shapes to help with centering and fitting to screen
        public static Rect CalculateBoundingBox(IEnumerable<Shape> shapes)
        {
            if (shapes == null) return new Rect();
            
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            bool hasShapes = false;
            
            foreach (var shape in shapes)
            {
                if (shape is Line line)
                {
                    minX = Math.Min(minX, Math.Min(line.X1, line.X2));
                    minY = Math.Min(minY, Math.Min(line.Y1, line.Y2));
                    maxX = Math.Max(maxX, Math.Max(line.X1, line.X2));
                    maxY = Math.Max(maxY, Math.Max(line.Y1, line.Y2));
                    hasShapes = true;
                }
            }
            
            if (!hasShapes) return new Rect(0, 0, 500, 500); // Default size if no shapes
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
