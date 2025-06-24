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
            }

            return shapes;
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
            canvas.Background = Brushes.WhiteSmoke;
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
