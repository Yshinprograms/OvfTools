using Microsoft.VisualStudio.TestTools.UnitTesting;
using netDxf;
using netDxf.Entities;
using OpenVectorFormat;
using OvfAnnotator;
using System.Collections.Generic;
using System.Linq;

namespace OvfAnnotatorTests {
    [TestClass]
    public class OvfToDxfConverterTests {
        #region Test Helpers
        private WorkPlane CreateSimpleWorkPlane() {
            var wp = new WorkPlane();
            // Block 0, assigned to Part 1
            wp.VectorBlocks.Add(new VectorBlock {
                LineSequence = new VectorBlock.Types.LineSequence { Points = { 0, 0, 10, 10 } },
                MetaData = new VectorBlock.Types.VectorBlockMetaData { PartKey = 1 }
            });
            // Block 1, assigned to Part 2
            wp.VectorBlocks.Add(new VectorBlock {
                LineSequence = new VectorBlock.Types.LineSequence { Points = { 20, 20, 30, 30 } },
                MetaData = new VectorBlock.Types.VectorBlockMetaData { PartKey = 2 }
            });
            return wp;
        }

        private WorkPlane CreateWorkPlaneWithInvalidHatchData() {
            var wp = new WorkPlane();
            // This hatch list has 6 points, not a multiple of 4.
            // It should create one valid hatch and ignore the last two points.
            wp.VectorBlocks.Add(new VectorBlock {
                Hatches = new VectorBlock.Types.Hatches { Points = { 0, 0, 1, 1, 2, 2 } }
            });
            return wp;
        }

        private WorkPlane CreateComplexWorkPlane() {
            var wp = CreateSimpleWorkPlane();
            // Block 2, assigned to Part 0 (unassigned)
            wp.VectorBlocks.Add(new VectorBlock {
                LineSequence = new VectorBlock.Types.LineSequence { Points = { 40, 40, 50, 50 } },
                MetaData = new VectorBlock.Types.VectorBlockMetaData { PartKey = 0 }
            });
            // Block 3, no metadata at all
            wp.VectorBlocks.Add(new VectorBlock {
                LineSequence = new VectorBlock.Types.LineSequence { Points = { 60, 60, 70, 70 } }
            });
            return wp;
        }
        private WorkPlane CreateWorkPlaneWithHatches() {
            var wp = new WorkPlane();
            wp.VectorBlocks.Add(new VectorBlock {
                Hatches = new VectorBlock.Types.Hatches { Points = { 0, 0, 10, 10, 20, 0, 30, 10 } }
            });
            return wp;
        }

        private WorkPlane CreateWorkPlaneWithUnsupportedTypes() {
            var wp = new WorkPlane();
            wp.VectorBlocks.Add(new VectorBlock { PointSequence = new VectorBlock.Types.PointSequence() });
            wp.VectorBlocks.Add(new VectorBlock { Arcs = new VectorBlock.Types.Arcs() });
            return wp;
        }

        private WorkPlane CreateWorkPlaneWithInvalidGeometry() {
            var wp = new WorkPlane();
            // This LineSequence has only one point, so it cannot form any lines.
            wp.VectorBlocks.Add(new VectorBlock { LineSequence = new VectorBlock.Types.LineSequence { Points = { 5, 5 } } });
            return wp;
        }

        private WorkPlane CreateWorkPlaneWithOverlappingBlocks() {
            var wp = new WorkPlane();
            // These two blocks are so close their centers will overlap.
            wp.VectorBlocks.Add(new VectorBlock { LineSequence = new VectorBlock.Types.LineSequence { Points = { 0, 0, 1, 1 } } });
            wp.VectorBlocks.Add(new VectorBlock { LineSequence = new VectorBlock.Types.LineSequence { Points = { 0, 0, 1, 1 } } });
            return wp;
        }
        #endregion

        #region "Color by Part" Tests (Default Behavior)

        [TestMethod]
        public void Convert_ColorByPart_GeneratesCorrectLayersAndEntities() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateSimpleWorkPlane();
            var options = new Options { ColorByBlock = false, TextHeight = 1.0 };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            Assert.IsTrue(resultDxf.Layers.Contains("Part_1"), "Layer for Part 1 was not created.");
            Assert.IsTrue(resultDxf.Layers.Contains("Part_2"), "Layer for Part 2 was not created.");
            Assert.IsTrue(resultDxf.Layers.Contains("Part_Annotations"), "Annotation layer was not created.");
            Assert.AreEqual(1, resultDxf.Entities.All.Count(e => e.Layer.Name == "Part_1"));
            Assert.AreEqual(1, resultDxf.Entities.All.Count(e => e.Layer.Name == "Part_2"));
            Assert.IsNotNull(resultDxf.Entities.Texts.FirstOrDefault(t => t.Value == "Part 1"), "Annotation for Part 1 not found.");
        }

        [TestMethod]
        public void Convert_ColorByPart_HandlesUnassignedBlocksGracefully() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateComplexWorkPlane();
            var options = new Options { ColorByBlock = false };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            Assert.IsTrue(resultDxf.Layers.Contains("Unassigned_Geometry"), "Layer for unassigned blocks was not created.");
            // 2 blocks are unassigned (PartKey 0, and null MetaData which defaults to 0)
            Assert.AreEqual(2, resultDxf.Entities.All.Count(e => e.Layer.Name == "Unassigned_Geometry"));
            // No annotation should be created for Part 0
            Assert.IsNull(resultDxf.Entities.Texts.FirstOrDefault(t => t.Value == "Part 0"), "Annotation for Part 0 should not exist.");
        }

        #endregion

        #region "Color by Block" Tests (--by-block flag)

        [TestMethod]
        public void Convert_ColorByBlock_GeneratesCorrectLayersAndAnnotations() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateSimpleWorkPlane();
            var options = new Options { ColorByBlock = true, TextHeight = 1.0, SimpleId = false };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            Assert.IsTrue(resultDxf.Layers.Contains("Geometry"), "Geometry layer was not created.");
            Assert.IsTrue(resultDxf.Layers.Contains("Annotations"), "Annotations layer was not created.");
            Assert.IsFalse(resultDxf.Layers.Contains("Part_1"), "Part-specific layers should not be created in by-block mode.");
            // 2 blocks = 2 geometry entities + 2 annotation entities
            Assert.AreEqual(4, resultDxf.Entities.All.Count());
            Assert.AreEqual(2, resultDxf.Entities.Texts.Count(), "Should be one annotation per block.");
            Assert.IsTrue(resultDxf.Entities.Texts.First().Value.StartsWith("ID:"), "Annotation should use the 'ID:' prefix by default.");
        }

        [TestMethod]
        public void Convert_WithSimpleIdOption_RemovesIdPrefix() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateSimpleWorkPlane();
            var options = new Options { ColorByBlock = true, SimpleId = true }; // The flag we're testing

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            var firstAnnotation = resultDxf.Entities.Texts.FirstOrDefault();
            Assert.IsNotNull(firstAnnotation);
            Assert.AreEqual("0", firstAnnotation.Value, "Annotation should be a simple number, not 'ID: 0'.");
            Assert.IsFalse(firstAnnotation.Value.Contains("ID:"), "Annotation should not contain the 'ID:' prefix.");
        }

        [TestMethod]
        public void Convert_WithTextHeightOption_SetsCorrectAnnotationHeight() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateSimpleWorkPlane();
            var options = new Options { ColorByBlock = true, TextHeight = 5.5 }; // The flag we're testing

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            var firstAnnotation = resultDxf.Entities.Texts.FirstOrDefault();
            Assert.IsNotNull(firstAnnotation);
            Assert.AreEqual(5.5, firstAnnotation.Height, 0.001);
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void Convert_WithEmptyWorkPlane_ReturnsEmptyDxfWithoutCrashing() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = new WorkPlane(); // No vector blocks
            var options = new Options();

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            Assert.IsNotNull(resultDxf, "DXF document should not be null.");
            Assert.AreEqual(0, resultDxf.Entities.All.Count(), "There should be no entities in the DXF file.");
        }
        #endregion
        #region New Edge Case and Coverage Tests

        [TestMethod]
        public void Convert_WithHatchGeometry_CreatesLineEntities() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateWorkPlaneWithHatches();
            var options = new Options { ColorByBlock = true };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            // The hatch data defines two separate lines.
            Assert.AreEqual(2, resultDxf.Entities.Lines.Count(), "Should create 2 Line entities for the 2 hatches.");
            Assert.AreEqual(3, resultDxf.Entities.All.Count(), "Should be 2 lines + 1 annotation.");
        }

        [TestMethod]
        public void Convert_WithUnsupportedVectorTypes_DoesNotCrashAndCreatesNoGeometry() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateWorkPlaneWithUnsupportedTypes();
            var options = new Options { ColorByBlock = true };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            // It should process both blocks, find no geometry, and create no annotations for them.
            Assert.AreEqual(0, resultDxf.Entities.All.Count(), "Should not create any entities for unsupported types.");
        }

        [TestMethod]
        public void Convert_WithInvalidVectorData_DoesNotCrashAndCreatesNoGeometry() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateWorkPlaneWithInvalidGeometry();
            var options = new Options { ColorByBlock = true };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            Assert.AreEqual(0, resultDxf.Entities.All.Count(), "Should not create entities from invalid geometry (e.g., a 1-point line).");
        }

        [TestMethod]
        public void Convert_ByBlockWithOverlappingAnnotations_NudgesLabelsApart() {
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateWorkPlaneWithOverlappingBlocks();
            var options = new Options { ColorByBlock = true, TextHeight = 1.0 };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            var texts = resultDxf.Entities.Texts.ToList();
            Assert.AreEqual(2, texts.Count);

            var pos0 = texts[0].Position;
            var pos1 = texts[1].Position;

            // Both start at the same center (0.5, 0.5). The first is placed,
            // the second is nudged down by `textHeight * 1.5`.
            Assert.AreEqual(0.5, pos0.X, 0.001);
            Assert.AreEqual(0.5, pos0.Y, 0.001);
            Assert.AreEqual(0.5, pos1.X, 0.001);
            Assert.AreEqual(0.5 - 1.5, pos1.Y, 0.001, "The second label was not nudged correctly.");
        }

        #endregion
        #region Final Coverage and Edge Case Tests

        [TestMethod]
        public void GetBlockGeometry_WithInvalidHatchData_HandlesGracefully() {
            // This tests that our hatch logic doesn't crash on malformed data.
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateWorkPlaneWithInvalidHatchData();
            var options = new Options { ColorByBlock = true };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            // It should successfully create 1 line from the first 4 points and ignore the rest.
            Assert.AreEqual(1, resultDxf.Entities.Lines.Count(), "Should create one valid line from the malformed hatch data.");
        }

        [TestMethod]
        public void FindAvailableLabelPosition_WithIdenticalPositions_NudgesCorrectly() {
            // This tests the core "nudge" logic of the label positioner.
            // ARRANGE
            var converter = new OvfToDxfConverter();
            var workPlane = CreateWorkPlaneWithOverlappingBlocks(); // Reusing this helper!
            var options = new Options { ColorByBlock = true, TextHeight = 1.0 };

            // ACT
            var resultDxf = converter.Convert(workPlane, options);

            // ASSERT
            var texts = resultDxf.Entities.Texts.OrderBy(t => t.Position.Y).ToList(); // Order by Y to get them in a predictable sequence
            Assert.AreEqual(2, texts.Count);

            var nudgedPos = texts[0].Position; // The one with the smaller Y value was nudged
            var originalPos = texts[1].Position;

            Assert.AreEqual(0.5, originalPos.Y, 0.001);
            Assert.AreEqual(0.5 - 1.5, nudgedPos.Y, 0.001, "The second label was not nudged correctly.");
        }

        #endregion
    }
}