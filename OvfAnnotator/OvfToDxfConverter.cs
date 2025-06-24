// OvfToDxfConverter.cs
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using OpenVectorFormat;

namespace OvfAnnotator {
    public class OvfToDxfConverter {
        public DxfDocument Convert(WorkPlane workPlane) {
            var dxf = new DxfDocument();

            var geometryLayer = new Layer("Geometry") { Color = AciColor.Blue };
            var annotationLayer = new Layer("Annotations") { Color = AciColor.DarkGray };

            dxf.Layers.Add(geometryLayer);
            dxf.Layers.Add(annotationLayer);

            int blockId = 0;
            foreach (var block in workPlane.VectorBlocks) {
                var entities = ProcessVectorBlock(block, blockId, geometryLayer, annotationLayer);

                if (entities != null) {
                    dxf.Entities.Add(entities);
                }

                blockId++;
            }

            return dxf;
        }

        private List<EntityObject>? ProcessVectorBlock(VectorBlock block, int blockId, Layer geometryLayer, Layer annotationLayer) {
            var createdEntities = new List<EntityObject>();
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            void UpdateBounds(double x, double y) {
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            switch (block.VectorDataCase) {
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    var points = block.LineSequence.Points;
                    var vertices = new List<Polyline2DVertex>();
                    for (int i = 0; i < points.Count; i += 2) {
                        double x = points[i];
                        double y = points[i + 1];
                        vertices.Add(new Polyline2DVertex(x, y));
                        UpdateBounds(x, y);
                    }
                    var polyline = new Polyline2D(vertices) { Layer = geometryLayer };
                    createdEntities.Add(polyline);
                    break;

                case VectorBlock.VectorDataOneofCase.Hatches:
                    var hatchPoints = block.Hatches.Points;
                    for (int i = 0; i < hatchPoints.Count; i += 4) {
                        var startPoint = new Vector2(hatchPoints[i], hatchPoints[i + 1]);
                        var endPoint = new Vector2(hatchPoints[i + 2], hatchPoints[i + 3]);

                        var line = new Line(startPoint, endPoint) { Layer = geometryLayer };
                        createdEntities.Add(line);

                        UpdateBounds(startPoint.X, startPoint.Y);
                        UpdateBounds(endPoint.X, endPoint.Y);
                    }
                    break;

                default:
                    Console.WriteLine($"  -> Skipping block {blockId} of unhandled type: {block.VectorDataCase}");
                    return null;
            }

            if (maxX > double.MinValue) {
                var annotationText = new Text($"ID: {blockId}", new Vector2(maxX, maxY), 1.0) {
                    Layer = annotationLayer
                };
                createdEntities.Add(annotationText);
            }

            return createdEntities;
        }
    }
}