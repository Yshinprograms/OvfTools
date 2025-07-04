using NetTopologySuite.Geometries;
using OpenVectorFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using VectorBlock = OpenVectorFormat.VectorBlock;

namespace OvfParameterModifier.Services {
    public class PartAssignerService {
        // LEVEL 1: The highest level of abstraction. Reads like a summary of the whole process.
        public void AssignParts(Job job) {
            var activePartsGeometry = new Dictionary<int, Geometry>();

            foreach (var workPlane in job.WorkPlanes.OrderBy(wp => wp.ZPosInMm)) {
                activePartsGeometry = ProcessWorkPlane(workPlane, activePartsGeometry, job);
            }
        }

        // LEVEL 2: Processes one layer. Its story is clear: "For each contour, assign it."
        private Dictionary<int, Geometry> ProcessWorkPlane(WorkPlane workPlane, Dictionary<int, Geometry> previousLayerParts, Job job) {
            var currentLayerParts = new Dictionary<int, Geometry>();
            var contours = GetContoursFromWorkPlane(workPlane);

            foreach (var contourBlock in contours) {
                AssignContourToPart(contourBlock, previousLayerParts, currentLayerParts, job);
            }

            return currentLayerParts;
        }

        // LEVEL 3: The most detailed level. Handles one single contour.
        private void AssignContourToPart(VectorBlock contourBlock, Dictionary<int, Geometry> previousLayerParts, Dictionary<int, Geometry> currentLayerParts, Job job) {
            var ntsPolygon = CreateNtsPolygonFromVectorBlock(contourBlock);
            if (ntsPolygon == null || !ntsPolygon.IsValid) return; // Skip invalid geometry

            // Ensure MetaData exists before trying to set PartKey
            if (contourBlock.MetaData == null) {
                contourBlock.MetaData = new VectorBlock.Types.VectorBlockMetaData();
            }

            // Try to find a parent part by checking for intersection
            foreach (var parentPart in previousLayerParts) {
                if (parentPart.Value.Intersects(ntsPolygon)) {
                    int partKey = parentPart.Key;
                    contourBlock.MetaData.PartKey = partKey;

                    // Update this part's geometry on the current layer
                    if (currentLayerParts.TryGetValue(partKey, out Geometry existingGeo)) {
                        currentLayerParts[partKey] = existingGeo.Union(ntsPolygon);
                    } else {
                        currentLayerParts.Add(partKey, ntsPolygon);
                    }
                    return; // Found our parent, the job for this contour is done.
                }
            }

            // If we get here, no parent was found. This must be a new part.
            int newPartKey = GetNextAvailablePartKey(job);
            job.PartsMap.Add(newPartKey, new Part { Name = $"Part-{newPartKey}" });
            contourBlock.MetaData.PartKey = newPartKey;
            currentLayerParts.Add(newPartKey, ntsPolygon);
        }

        // --- Helper Methods ---

        private int GetNextAvailablePartKey(Job job) {
            if (job.PartsMap.Keys.Count == 0) return 1;
            return job.PartsMap.Keys.Max() + 1;
        }

        private IEnumerable<VectorBlock> GetContoursFromWorkPlane(WorkPlane workPlane) {
            return workPlane.VectorBlocks.Where(b => b.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence);
        }

        private Polygon CreateNtsPolygonFromVectorBlock(VectorBlock block) {
            if (block.VectorDataCase != VectorBlock.VectorDataOneofCase.LineSequence) {
                return null; // We can only make polygons from LineSequences.
            }

            var points = block.LineSequence.Points;

            // A valid polygon needs at least 3 vertices to form a shape (e.g., a triangle),
            // which means at least 6 float values in our list.
            if (points.Count < 6) {
                return null;
            }

            // NTS works with Coordinate objects, so let's create a list of them.
            var coordinates = new List<Coordinate>();
            for (int i = 0; i < points.Count; i += 2) {
                coordinates.Add(new Coordinate(points[i], points[i + 1]));
            }

            // NTS Polygons require the first and last points to be identical (a "closed ring").
            // Let's ensure this is true.
            if (!coordinates[0].Equals(coordinates[coordinates.Count - 1])) {
                coordinates.Add(new Coordinate(coordinates[0])); // Add a copy of the first point to the end.
            }

            // A closed ring for a polygon must have at least 4 points (e.g., A->B->C->A).
            if (coordinates.Count < 4) {
                return null;
            }

            // The GeometryFactory is the standard way to create shapes in NTS.
            var geometryFactory = new GeometryFactory();
            var shell = geometryFactory.CreateLinearRing(coordinates.ToArray());
            var polygon = geometryFactory.CreatePolygon(shell);

            return polygon;
        }
    }
}