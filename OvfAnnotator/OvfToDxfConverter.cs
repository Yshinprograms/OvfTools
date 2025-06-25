using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using OpenVectorFormat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OvfAnnotator {
    public class OvfToDxfConverter {
        public DxfDocument Convert(WorkPlane workPlane, Options options) {
            var dxf = new DxfDocument();
            var colorGenerator = new ColorGenerator();
            var labelPositions = new List<Vector3>();
            var geometryLayer = new Layer("Geometry") { Color = AciColor.Default };
            var annotationLayer = new Layer("Annotations") { Color = AciColor.Default };
            dxf.Layers.Add(geometryLayer);
            dxf.Layers.Add(annotationLayer);
            var allEntities = new List<EntityObject>();

            for (int i = 0; i < workPlane.VectorBlocks.Count; i++) {
                var block = workPlane.VectorBlocks[i];
                var color = colorGenerator.GetNextColor();
                // We pass the 'options' down to the next method.
                var processedEntities = ProcessBlock(block, i, color, geometryLayer, annotationLayer, labelPositions, options);
                if (processedEntities.Any()) {
                    allEntities.AddRange(processedEntities);
                }
            }
            dxf.Entities.Add(allEntities);
            return dxf;
        }

        private List<EntityObject> ProcessBlock(VectorBlock block, int blockId, AciColor color, Layer geoLayer, Layer annoLayer, List<Vector3> labelPositions, Options options) {
            var entities = new List<EntityObject>();
            var geometry = GetBlockGeometry(block, geoLayer, color, out double minX, out double minY, out double maxX, out double maxY);

            if (!geometry.Any()) {
                Console.WriteLine($"  -> Skipping block {blockId} of unhandled type: {block.VectorDataCase}");
                return entities;
            }

            var annotation = CreateAnnotation(blockId, minX, minY, maxX, maxY, color, annoLayer, labelPositions, options);
            entities.AddRange(geometry);
            entities.Add(annotation);
            labelPositions.Add(annotation.Position);
            return entities;
        }

        private Text CreateAnnotation(int blockId, double minX, double minY, double maxX, double maxY, AciColor color, Layer layer, List<Vector3> existingPositions, Options options) {
            double textHeight = options.TextHeight;

            double centerX = minX + (maxX - minX) / 2.0;
            double centerY = minY + (maxY - minY) / 2.0;
            var initialPosition = new Vector3(centerX, centerY, 0);

            var finalPosition = FindAvailableLabelPosition(initialPosition, textHeight, existingPositions);

            string labelText = options.SimpleId ? $"ID: {blockId}" : $"{blockId}";

            return new Text(labelText, finalPosition, textHeight) {
                Layer = layer,
                Alignment = TextAlignment.MiddleLeft,
                Color = color
            };
        }

        private List<EntityObject> GetBlockGeometry(VectorBlock block, Layer layer, AciColor color, out double minX, out double minY, out double maxX, out double maxY) {
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
                    entities.Add(new Polyline2D(vertices) { Layer = layer, Color = color });
                    break;
                case VectorBlock.VectorDataOneofCase.Hatches:
                    var hatchPoints = block.Hatches.Points;
                    for (int i = 0; i < hatchPoints.Count; i += 4) {
                        var start = new Vector2(hatchPoints[i], hatchPoints[i + 1]);
                        var end = new Vector2(hatchPoints[i + 2], hatchPoints[i + 3]);
                        entities.Add(new Line(start, end) { Layer = layer, Color = color });
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
        private Vector3 FindAvailableLabelPosition(Vector3 desiredPosition, double textHeight, List<Vector3> existingPositions) {
            var finalPosition = new Vector3(desiredPosition.X, desiredPosition.Y, desiredPosition.Z);
            bool isOccupied;
            do {
                isOccupied = false;
                foreach (var existingPos in existingPositions) {
                    if (Vector3.Distance(finalPosition, existingPos) < textHeight) {
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