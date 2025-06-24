// OvfToDxfConverter.cs

using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using OpenVectorFormat;
using System;
using System.Collections.Generic;

namespace OvfAnnotator {
    /// <summary>
    /// Handles the conversion of an OVF WorkPlane into an annotated DxfDocument.
    /// </summary>
    public class OvfToDxfConverter {
        /// <summary>
        /// Converts a single OVF WorkPlane to a DxfDocument, including geometry and annotations.
        /// This is the main public entry point for this class.
        /// </summary>
        /// <param name="workPlane">The OVF WorkPlane to convert.</param>
        /// <returns>A fully populated and annotated DxfDocument.</returns>
        public DxfDocument Convert(WorkPlane workPlane) {
            var dxf = new DxfDocument();
            // We set the layer color to default, as individual entities will be colored.
            var geometryLayer = new Layer("Geometry") { Color = AciColor.Default };
            var annotationLayer = new Layer("Annotations") { Color = AciColor.Default };
            dxf.Layers.Add(geometryLayer);
            dxf.Layers.Add(annotationLayer);

            var labelPositions = new List<Vector2>();
            var colorGenerator = new ColorGenerator();

            int blockId = 0;
            foreach (var block in workPlane.VectorBlocks) {
                // Get the unique, high-contrast color for this block.
                var currentColor = colorGenerator.GetNextColor();

                // Pass the color down to the geometry creation method.
                var geometryEntities = GetBlockGeometry(block, geometryLayer, currentColor, out double minX, out double minY, out double maxX, out double maxY);

                if (geometryEntities.Count > 0) {
                    dxf.Entities.Add(geometryEntities);

                    const double textHeight = 1.0;
                    var initialPosition = new Vector2(minX + (maxX - minX) / 2.0, minY + (maxY - minY) / 2.0);
                    var finalPosition = FindAvailableLabelPosition(initialPosition, textHeight, labelPositions);

                    var annotationText = new Text($"ID: {blockId}", finalPosition, textHeight) {
                        Layer = annotationLayer,
                        Alignment = TextAlignment.MiddleLeft,
                        // Apply the same unique color to the text for clear association.
                        Color = currentColor
                    };
                    dxf.Entities.Add(annotationText);

                    labelPositions.Add(finalPosition);
                } else {
                    Console.WriteLine($"  -> Skipping block {blockId} of unhandled type: {block.VectorDataCase}");
                }
                blockId++;
            }
            return dxf;
        }

        /// <summary>
        /// Creates a list of colored DXF geometry entities from a single VectorBlock and calculates its bounding box.
        /// </summary>
        private List<EntityObject> GetBlockGeometry(VectorBlock block, Layer layer, AciColor color, out double minX, out double minY, out double maxX, out double maxY) {
            var entities = new List<EntityObject>();
            minX = double.MaxValue;
            minY = double.MaxValue;
            maxX = double.MinValue;
            maxY = double.MinValue;

            switch (block.VectorDataCase) {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    var points = block.LineSequence.Points;
                    var vertices = new List<Polyline2DVertex>();
                    for (int i = 0; i < points.Count; i += 2) {
                        double x = points[i];
                        double y = points[i + 1];
                        vertices.Add(new Polyline2DVertex(x, y));

                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                    var polyline = new Polyline2D(vertices) { Layer = layer, Color = color };
                    entities.Add(polyline);
                    break;

                case VectorBlock.VectorDataOneofCase.Hatches:
                    var hatchPoints = block.Hatches.Points;
                    for (int i = 0; i < hatchPoints.Count; i += 4) {
                        var startPoint = new Vector2(hatchPoints[i], hatchPoints[i + 1]);
                        var endPoint = new Vector2(hatchPoints[i + 2], hatchPoints[i + 3]);
                        var line = new Line(startPoint, endPoint) { Layer = layer, Color = color };
                        entities.Add(line);

                        if (startPoint.X < minX) minX = startPoint.X;
                        if (startPoint.Y < minY) minY = startPoint.Y;
                        if (startPoint.X > maxX) maxX = startPoint.X;
                        if (startPoint.Y > maxY) maxY = startPoint.Y;

                        if (endPoint.X < minX) minX = endPoint.X;
                        if (endPoint.Y < minY) minY = endPoint.Y;
                        if (endPoint.X > maxX) maxX = endPoint.X;
                        if (endPoint.Y > maxY) maxY = endPoint.Y;
                    }
                    break;
            }
            return entities;
        }

        /// <summary>
        /// Finds an available position for a new label, avoiding collisions with existing labels.
        /// </summary>
        private Vector2 FindAvailableLabelPosition(Vector2 desiredPosition, double textHeight, List<Vector2> existingPositions) {
            var finalPosition = new Vector2(desiredPosition.X, desiredPosition.Y);
            bool isOccupied;

            do {
                isOccupied = false;
                foreach (var existingPos in existingPositions) {
                    if (Vector2.Distance(finalPosition, existingPos) < textHeight) {
                        isOccupied = true;
                        finalPosition.Y -= textHeight * 1.5;
                        break;
                    }
                }
            } while (isOccupied);

            return finalPosition;
        }
    }
}