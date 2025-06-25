using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using OpenVectorFormat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OvfAnnotator {
    public class OvfToDxfConverter {
        /// <summary>
        /// Converts a WorkPlane to a DxfDocument, routing to the correct coloring strategy.
        /// </summary>
        public DxfDocument Convert(WorkPlane workPlane, Options options) {
            var dxf = new DxfDocument();

            // Route to the correct strategy based on the user's command-line option.
            if (options.ColorByBlock) {
                ProcessWorkPlaneByBlock(dxf, workPlane, options);
            } else {
                ProcessWorkPlaneByPart(dxf, workPlane, options);
            }

            return dxf;
        }

        /// <summary>
        /// Strategy 1: Colors each VectorBlock individually and gives it a unique ID label.
        /// </summary>
        private void ProcessWorkPlaneByBlock(DxfDocument dxf, WorkPlane workPlane, Options options) {
            var colorGenerator = new ColorGenerator();
            var labelPositions = new List<Vector3>();
            var geometryLayer = new Layer("Geometry") { Color = AciColor.Default };
            var annotationLayer = new Layer("Annotations") { Color = AciColor.Default };
            dxf.Layers.Add(geometryLayer);
            dxf.Layers.Add(annotationLayer);

            for (int i = 0; i < workPlane.VectorBlocks.Count; i++) {
                var block = workPlane.VectorBlocks[i];
                var color = colorGenerator.GetNextColor();
                var geometry = GetBlockGeometry(block, color, out double minX, out double minY, out double maxX, out double maxY);

                if (!geometry.Any()) continue;

                var annotation = CreateBlockAnnotation(i, minX, minY, maxX, maxY, color, annotationLayer, labelPositions, options);

                // Assign all geometry from this block to the main geometry layer
                foreach (var entity in geometry) { entity.Layer = geometryLayer; }

                dxf.Entities.Add(geometry);
                dxf.Entities.Add(annotation);
                labelPositions.Add(annotation.Position);
            }
        }

        /// <summary>
        /// Strategy 2 (Default): Colors geometry based on its assigned Part ID and places it on a part-specific layer.
        /// </summary>
        private void ProcessWorkPlaneByPart(DxfDocument dxf, WorkPlane workPlane, Options options) {
            var colorGenerator = new ColorGenerator();
            var partLayers = new Dictionary<int, Layer>();
            var partColors = new Dictionary<int, AciColor>();

            foreach (var block in workPlane.VectorBlocks) {
                // Default to Part 0 (or a special "unassigned" key) if no PartKey is present.
                int partKey = block.MetaData?.PartKey ?? 0;

                // Get or create a consistent color for this Part ID.
                if (!partColors.ContainsKey(partKey)) {
                    partColors[partKey] = colorGenerator.GetNextColor();
                }
                var color = partColors[partKey];

                // Get or create a dedicated DXF layer for this Part ID.
                if (!partLayers.ContainsKey(partKey)) {
                    string layerName = (partKey == 0) ? "Unassigned_Geometry" : $"Part_{partKey}";
                    var newLayer = new Layer(layerName) { Color = color };
                    dxf.Layers.Add(newLayer);
                    partLayers[partKey] = newLayer;
                }
                var layer = partLayers[partKey];

                var geometry = GetBlockGeometry(block, color, out _, out _, out _, out _);

                // Assign all geometry from this block to its corresponding part layer.
                foreach (var entity in geometry) { entity.Layer = layer; }
                dxf.Entities.Add(geometry);
            }
        }

        /// <summary>
        /// Generic helper to create DXF entities from a VectorBlock's data.
        /// </summary>
        private List<EntityObject> GetBlockGeometry(VectorBlock block, AciColor color, out double minX, out double minY, out double maxX, out double maxY) {
            var entities = new List<EntityObject>();
            minX = double.MaxValue; minY = double.MaxValue; maxX = double.MinValue; maxY = double.MinValue;

            switch (block.VectorDataCase) {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    var points = block.LineSequence.Points;
                    var vertices = new List<Polyline2DVertex>();
                    for (int i = 0; i < points.Count; i += 2) {
                        double x = points[i];
                        double y = points[i + 1];
                        vertices.Add(new Polyline2DVertex(new Vector2(x, y)));
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                    if (vertices.Any())
                        entities.Add(new Polyline2D(vertices) { Color = color });
                    break;

                case VectorBlock.VectorDataOneofCase.Hatches:
                    var hatchPoints = block.Hatches.Points;
                    for (int i = 0; i < hatchPoints.Count; i += 4) {
                        var start = new Vector2(hatchPoints[i], hatchPoints[i + 1]);
                        var end = new Vector2(hatchPoints[i + 2], hatchPoints[i + 3]);
                        entities.Add(new Line(start, end) { Color = color });
                        if (start.X < minX) minX = start.X;
                        if (start.Y < minY) minY = start.Y;
                        if (start.X > maxX) maxX = start.X;
                        if (start.Y > maxY) maxY = start.Y;
                        if (end.X < minX) minX = end.X;
                        if (end.Y < minY) minY = end.Y;
                        if (end.X > maxX) maxX = end.X;
                        if (end.Y > maxY) maxY = end.Y;
                    }
                    break;
            }
            return entities;
        }

        /// <summary>
        /// Creates a text annotation for a block's ID, used only in "by block" mode.
        /// </summary>
        private Text CreateBlockAnnotation(int blockId, double minX, double minY, double maxX, double maxY, AciColor color, Layer layer, List<Vector3> existingPositions, Options options) {
            double textHeight = options.TextHeight;
            double centerX = minX + (maxX - minX) / 2.0;
            double centerY = minY + (maxY - minY) / 2.0;
            var initialPosition = new Vector3(centerX, centerY, 0);

            var finalPosition = FindAvailableLabelPosition(initialPosition, textHeight, existingPositions);

            string labelText = options.SimpleId ? $"{blockId}" : $"ID: {blockId}";

            return new Text(labelText, finalPosition, textHeight) {
                Layer = layer,
                Alignment = TextAlignment.MiddleLeft,
                Color = color
            };
        }

        /// <summary>
        /// Finds an available position for a text label to prevent labels from overlapping.
        /// </summary>
        private Vector3 FindAvailableLabelPosition(Vector3 desiredPosition, double textHeight, List<Vector3> existingPositions) {
            var finalPosition = new Vector3(desiredPosition.X, desiredPosition.Y, desiredPosition.Z);
            bool isOccupied;
            do {
                isOccupied = false;
                foreach (var existingPos in existingPositions) {
                    if (Vector3.Distance(finalPosition, existingPos) < textHeight) {
                        isOccupied = true;
                        finalPosition.Y -= textHeight * 1.5; // Nudge the label down
                        break;
                    }
                }
            } while (isOccupied);
            return finalPosition;
        }
    }
}