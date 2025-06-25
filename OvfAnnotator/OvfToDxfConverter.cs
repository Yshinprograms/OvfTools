using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using OpenVectorFormat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OvfAnnotator {
    public class OvfToDxfConverter {
        // A private struct to handle bounding box calculations. Perfect as-is.
        private struct SimpleBounds {
            public double MinX, MinY, MaxX, MaxY;
            public SimpleBounds() { MinX = double.MaxValue; MinY = double.MaxValue; MaxX = double.MinValue; MaxY = double.MinValue; }
            public void Union(double x, double y) { if (x < MinX) MinX = x; if (y < MinY) MinY = y; if (x > MaxX) MaxX = x; if (y > MaxY) MaxY = y; }
            public Vector3 Center() => new Vector3(MinX + (MaxX - MinX) / 2.0, MinY + (MaxY - MinY) / 2.0, 0);
        }

        // --- PUBLIC ENTRY POINT ---
        public DxfDocument Convert(WorkPlane workPlane, Options options) {
            var dxf = new DxfDocument();
            if (options.ColorByBlock) {
                ProcessWorkPlaneByBlock(dxf, workPlane, options);
            } else {
                ProcessWorkPlaneByPart(dxf, workPlane, options);
            }
            return dxf;
        }

        // --- "BY BLOCK" STRATEGY ---
        private void ProcessWorkPlaneByBlock(DxfDocument dxf, WorkPlane workPlane, Options options) {
            var colorGenerator = new ColorGenerator();
            var labelPositions = new List<Vector3>();
            var geometryLayer = new Layer("Geometry") { Color = AciColor.Default };
            var annotationLayer = new Layer("Annotations") { Color = AciColor.Default };
            dxf.Layers.Add(geometryLayer);
            dxf.Layers.Add(annotationLayer);

            for (int i = 0; i < workPlane.VectorBlocks.Count; i++) {
                ProcessSingleBlock(dxf, workPlane.VectorBlocks[i], i, options, colorGenerator, geometryLayer, annotationLayer, labelPositions);
            }
        }

        private void ProcessSingleBlock(DxfDocument dxf, VectorBlock block, int blockId, Options options, ColorGenerator colorGenerator, Layer geoLayer, Layer annoLayer, List<Vector3> labelPositions) {
            var color = colorGenerator.GetNextColor();
            var geometry = GetBlockGeometry(block, color, out double minX, out double minY, out double maxX, out double maxY);

            if (!geometry.Any()) return;

            var annotation = CreateBlockAnnotation(blockId, minX, minY, maxX, maxY, color, annoLayer, labelPositions, options);

            foreach (var entity in geometry) {
                entity.Layer = geoLayer;
                dxf.Entities.Add(entity);
            }
            dxf.Entities.Add(annotation);
            labelPositions.Add(annotation.Position);
        }


        // --- "BY PART" STRATEGY ---
        private void ProcessWorkPlaneByPart(DxfDocument dxf, WorkPlane workPlane, Options options) {
            var partDataOnLayer = new Dictionary<int, (List<EntityObject> Geometry, SimpleBounds Bbox, AciColor Color, Layer Layer)>();
            GatherPartData(workPlane, dxf, partDataOnLayer);
            DrawAndAnnotateParts(dxf, partDataOnLayer, options);
        }

        private void GatherPartData(WorkPlane workPlane, DxfDocument dxf, Dictionary<int, (List<EntityObject> Geometry, SimpleBounds Bbox, AciColor Color, Layer Layer)> partData) {
            var colorGenerator = new ColorGenerator();
            foreach (var block in workPlane.VectorBlocks) {
                ProcessBlockForPartGrouping(block, dxf, partData, colorGenerator);
            }
        }

        private void ProcessBlockForPartGrouping(VectorBlock block, DxfDocument dxf, Dictionary<int, (List<EntityObject> Geometry, SimpleBounds Bbox, AciColor Color, Layer Layer)> partData, ColorGenerator colorGenerator) {
            int partKey = block.MetaData?.PartKey ?? 0;
            var currentPartData = GetOrAddPartData(partData, partKey, dxf, colorGenerator);

            var geometry = GetBlockGeometry(block, currentPartData.Color, out double minX, out double minY, out double maxX, out double maxY);
            if (!geometry.Any()) return;

            currentPartData.Geometry.AddRange(geometry);
            currentPartData.Bbox.Union(minX, minY);
            currentPartData.Bbox.Union(maxX, maxY);
        }

        private (List<EntityObject> Geometry, SimpleBounds Bbox, AciColor Color, Layer Layer) GetOrAddPartData(Dictionary<int, (List<EntityObject> Geometry, SimpleBounds Bbox, AciColor Color, Layer Layer)> partData, int partKey, DxfDocument dxf, ColorGenerator colorGenerator) {
            if (!partData.ContainsKey(partKey)) {
                string layerName = (partKey == 0) ? "Unassigned_Geometry" : $"Part_{partKey}";
                var color = colorGenerator.GetNextColor();
                var layer = new Layer(layerName) { Color = color };
                dxf.Layers.Add(layer);
                partData[partKey] = (new List<EntityObject>(), new SimpleBounds(), color, layer);
            }
            return partData[partKey];
        }

        private void DrawAndAnnotateParts(DxfDocument dxf, Dictionary<int, (List<EntityObject> Geometry, SimpleBounds Bbox, AciColor Color, Layer Layer)> partData, Options options) {
            var annotationLayer = new Layer("Part_Annotations") { Color = AciColor.Default };
            dxf.Layers.Add(annotationLayer);
            var labelPositions = new List<Vector3>();

            foreach (var entry in partData) {
                var data = entry.Value;
                foreach (var entity in data.Geometry) {
                    entity.Layer = data.Layer;
                    dxf.Entities.Add(entity);
                }

                if (entry.Key != 0) 
                {
                    CreatePartAnnotation(dxf, entry.Key, data, options, annotationLayer, labelPositions);
                }
            }
        }

        private void CreatePartAnnotation(DxfDocument dxf, int partKey, (List<EntityObject> Geometry, SimpleBounds Bbox, AciColor Color, Layer Layer) data, Options options, Layer annotationLayer, List<Vector3> labelPositions) {
            var center = data.Bbox.Center();
            var finalPosition = FindAvailableLabelPosition(center, options.TextHeight, labelPositions);
            string labelText = $"Part {partKey}";

            var annotation = new Text(labelText, finalPosition, options.TextHeight) {
                Layer = annotationLayer,
                Alignment = TextAlignment.MiddleCenter,
                Color = data.Color
            };
            dxf.Entities.Add(annotation);
            labelPositions.Add(finalPosition);
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