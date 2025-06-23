using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat;
using OvfParameterModifier;
using System;
using System.Collections.Generic;
using System.Linq;
using PartArea = OpenVectorFormat.VectorBlock.Types.PartArea; // FIX: Added type alias for the nested enum

namespace OvfParameterModifier.Tests {
    [TestClass]
    public class JobEditorTests {
        private JobEditor _editor;
        [TestInitialize]
        public void TestInitialize() {
            _editor = new JobEditor();
        }
        #region Test Helpers
        private Job CreateTestJob(int numLayers = 0, int blocksPerLayer = 0, IDictionary<int, MarkingParams> paramMap = null) {
            var job = new Job { JobMetaData = new Job.Types.JobMetaData { JobName = "Test Job" } };
            for (int i = 0; i < numLayers; i++) {
                var workPlane = new WorkPlane { WorkPlaneNumber = i };
                for (int j = 0; j < blocksPerLayer; j++) {
                    workPlane.VectorBlocks.Add(new VectorBlock { MarkingParamsKey = -1 });
                }
                job.WorkPlanes.Add(workPlane);
            }
            if (paramMap != null) {
                foreach (var pair in paramMap) {
                    job.MarkingParamsMap.Add(pair.Key, pair.Value);
                }
            }
            return job;
        }
        private VectorBlock CreateVectorBlock(int initialKey, PartArea? area = null) {
            var block = new VectorBlock { MarkingParamsKey = initialKey };
            if (area.HasValue) {
                block.LpbfMetadata = new VectorBlock.Types.LPBFMetadata { PartArea = area.Value };
            }
            return block;
        }
        #endregion
        #region GetMaxLayerIndex Tests (3 Tests)
        [TestMethod]
        public void GetMaxLayerIndex_JobWithFiveLayers_ReturnsFour() {
            var job = CreateTestJob(numLayers: 5);
            int result = _editor.GetMaxLayerIndex(job);
            Assert.AreEqual(4, result);
        }
        [TestMethod]
        public void GetMaxLayerIndex_JobWithOneLayer_ReturnsZero() {
            var job = CreateTestJob(numLayers: 1);
            int result = _editor.GetMaxLayerIndex(job);
            Assert.AreEqual(0, result);
        }
        [TestMethod]
        public void GetMaxLayerIndex_JobWithZeroLayers_ReturnsNegativeOne() {
            var job = CreateTestJob(numLayers: 0);
            int result = _editor.GetMaxLayerIndex(job);
            Assert.AreEqual(-1, result);
        }
        #endregion
        #region DoesParamSetExist Tests (4 Tests)
        [TestMethod]
        public void DoesParamSetExist_KeyIsPresent_ReturnsTrue() {
            var job = CreateTestJob(paramMap: new Dictionary<int, MarkingParams> { { 5, new MarkingParams() } });
            bool result = _editor.DoesParamSetExist(job, 5);
            Assert.IsTrue(result);
        }
        [TestMethod]
        public void DoesParamSetExist_KeyZeroIsPresent_ReturnsTrue() {
            var job = CreateTestJob(paramMap: new Dictionary<int, MarkingParams> { { 0, new MarkingParams() } });
            bool result = _editor.DoesParamSetExist(job, 0);
            Assert.IsTrue(result);
        }
        [TestMethod]
        public void DoesParamSetExist_KeyIsNotPresent_ReturnsFalse() {
            var job = CreateTestJob(paramMap: new Dictionary<int, MarkingParams> { { 5, new MarkingParams() } });
            bool result = _editor.DoesParamSetExist(job, 99);
            Assert.IsFalse(result);
        }
        [TestMethod]
        public void DoesParamSetExist_MapIsEmpty_ReturnsFalse() {
            var job = CreateTestJob();
            bool result = _editor.DoesParamSetExist(job, 1);
            Assert.IsFalse(result);
        }
        #endregion
        #region FindOrCreateParameterSetKey Tests (12 Tests)
        [TestMethod]
        public void FindOrCreateParameterSetKey_ExactMatchExists_ReturnsExistingKeyAndDoesNotModifyMap() {
            var paramMap = new Dictionary<int, MarkingParams> { { 10, new MarkingParams { LaserPowerInW = 100f, LaserSpeedInMmPerS = 500f } } };
            var job = CreateTestJob(paramMap: paramMap);
            int initialMapSize = job.MarkingParamsMap.Count;
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreEqual(10, resultKey);
            Assert.AreEqual(initialMapSize, job.MarkingParamsMap.Count, "Map size should not change when a match is found.");
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_MatchWithinToleranceExists_ReturnsExistingKey() {
            var paramMap = new Dictionary<int, MarkingParams> { { 12, new MarkingParams { LaserPowerInW = 100.0001f, LaserSpeedInMmPerS = 499.9999f } } };
            var job = CreateTestJob(paramMap: paramMap);
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreEqual(12, resultKey);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_MultipleMatchesExist_ReturnsFirstKey() {
            var paramMap = new Dictionary<int, MarkingParams>
            {
                { 8, new MarkingParams { LaserPowerInW = 100f, LaserSpeedInMmPerS = 500f } },
                { 15, new MarkingParams { LaserPowerInW = 100f, LaserSpeedInMmPerS = 500f } }
            };
            var job = CreateTestJob(paramMap: paramMap);
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreEqual(8, resultKey, "Should return the first key it finds.");
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_OnlyPowerMatches_CreatesNewKey() {
            var paramMap = new Dictionary<int, MarkingParams> { { 5, new MarkingParams { LaserPowerInW = 100f, LaserSpeedInMmPerS = 999f } } };
            var job = CreateTestJob(paramMap: paramMap);
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreNotEqual(5, resultKey, "Should create a new key if only one parameter matches.");
            Assert.IsTrue(resultKey > 5);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_OnlySpeedMatches_CreatesNewKey() {
            var paramMap = new Dictionary<int, MarkingParams> { { 5, new MarkingParams { LaserPowerInW = 999f, LaserSpeedInMmPerS = 500f } } };
            var job = CreateTestJob(paramMap: paramMap);
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreNotEqual(5, resultKey, "Should create a new key if only one parameter matches.");
            Assert.IsTrue(resultKey > 5);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_MapIsEmpty_CreatesKeyOneAndModifiesMap() {
            var job = CreateTestJob();
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreEqual(1, resultKey);
            Assert.AreEqual(1, job.MarkingParamsMap.Count);
            Assert.AreEqual(100f, job.MarkingParamsMap[1].LaserPowerInW);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_MapHasItemsNoMatch_CreatesMaxKeyPlusOne() {
            var paramMap = new Dictionary<int, MarkingParams> { { 5, new MarkingParams() }, { 10, new MarkingParams() } };
            var job = CreateTestJob(paramMap: paramMap);
            int initialMapSize = job.MarkingParamsMap.Count;
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreEqual(11, resultKey, "New key should be max existing key + 1.");
            Assert.AreEqual(initialMapSize + 1, job.MarkingParamsMap.Count);
            Assert.IsNotNull(job.MarkingParamsMap[11]);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_MapHasNonContiguousKeys_CreatesMaxKeyPlusOne() {
            var paramMap = new Dictionary<int, MarkingParams> { { 1, new MarkingParams() }, { 5, new MarkingParams() }, { 20, new MarkingParams() } };
            var job = CreateTestJob(paramMap: paramMap);
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreEqual(21, resultKey);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_MapHasOnlyKeyZero_CreatesKeyOne() {
            var paramMap = new Dictionary<int, MarkingParams> { { 0, new MarkingParams() } };
            var job = CreateTestJob(paramMap: paramMap);
            int resultKey = _editor.FindOrCreateParameterSetKey(job, 100f, 500f);
            Assert.AreEqual(1, resultKey);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_NoMatchExists_NewParamHasCorrectValues() {
            var job = CreateTestJob();
            int newKey = _editor.FindOrCreateParameterSetKey(job, 25.5f, 1200.1f);
            var newParam = job.MarkingParamsMap[newKey];
            Assert.AreEqual(25.5f, newParam.LaserPowerInW);
            Assert.AreEqual(1200.1f, newParam.LaserSpeedInMmPerS);
            Assert.AreEqual("P25.5W_S1200.1mmps", newParam.Name);
        }
        [TestMethod]
        public void FindOrCreateParameterSetKey_WithNegativeInputs_CreatesNewKeyWithNegativeValues() {
            var job = CreateTestJob();
            int newKey = _editor.FindOrCreateParameterSetKey(job, -50f, -200f);
            var newParam = job.MarkingParamsMap[newKey];
            Assert.AreEqual(-50f, newParam.LaserPowerInW);
            Assert.AreEqual(-200f, newParam.LaserSpeedInMmPerS);
        }
        #endregion
        #region ApplyParametersToLayerRange Tests (11 Tests)
        [TestMethod]
        public void ApplyParametersToLayerRange_ValidMiddleRange_UpdatesCorrectBlocksOnly() {
            var paramMap = new Dictionary<int, MarkingParams> { { 5, new MarkingParams() } };
            var job = CreateTestJob(numLayers: 5, blocksPerLayer: 2, paramMap: paramMap);
            _editor.ApplyParametersToLayerRange(job, 1, 3, 5);
            Assert.AreEqual(-1, job.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey, "Layer 0 should be unchanged.");
            Assert.AreEqual(5, job.WorkPlanes[1].VectorBlocks[0].MarkingParamsKey, "Layer 1 should be updated.");
            Assert.AreEqual(5, job.WorkPlanes[3].VectorBlocks[0].MarkingParamsKey, "Layer 3 should be updated.");
            Assert.AreEqual(-1, job.WorkPlanes[4].VectorBlocks[0].MarkingParamsKey, "Layer 4 should be unchanged.");
        }
        [TestMethod]
        public void ApplyParametersToLayerRange_SingleLayerRange_UpdatesCorrectBlocks() {
            var paramMap = new Dictionary<int, MarkingParams> { { 8, new MarkingParams() } };
            var job = CreateTestJob(numLayers: 3, blocksPerLayer: 1, paramMap: paramMap);
            _editor.ApplyParametersToLayerRange(job, 1, 1, 8);
            Assert.AreEqual(-1, job.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey);
            Assert.AreEqual(8, job.WorkPlanes[1].VectorBlocks[0].MarkingParamsKey);
            Assert.AreEqual(-1, job.WorkPlanes[2].VectorBlocks[0].MarkingParamsKey);
        }
        [TestMethod]
        public void ApplyParametersToLayerRange_FullLayerRange_UpdatesAllBlocks() {
            var paramMap = new Dictionary<int, MarkingParams> { { 9, new MarkingParams() } };
            var job = CreateTestJob(numLayers: 3, blocksPerLayer: 1, paramMap: paramMap);
            _editor.ApplyParametersToLayerRange(job, 0, 2, 9);
            Assert.AreEqual(9, job.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey);
            Assert.AreEqual(9, job.WorkPlanes[1].VectorBlocks[0].MarkingParamsKey);
            Assert.AreEqual(9, job.WorkPlanes[2].VectorBlocks[0].MarkingParamsKey);
        }
        [TestMethod]
        public void ApplyParametersToLayerRange_RangeIncludesLayerWithNoBlocks_DoesNotThrow() {
            var paramMap = new Dictionary<int, MarkingParams> { { 5, new MarkingParams() } };
            var job = CreateTestJob(numLayers: 3, blocksPerLayer: 1, paramMap: paramMap);
            job.WorkPlanes[1].VectorBlocks.Clear();
            _editor.ApplyParametersToLayerRange(job, 0, 2, 5);
            Assert.AreEqual(5, job.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey);
            Assert.AreEqual(0, job.WorkPlanes[1].VectorBlocks.Count);
            Assert.AreEqual(5, job.WorkPlanes[2].VectorBlocks[0].MarkingParamsKey);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyParametersToLayerRange_JobWithNoLayers_ThrowsArgumentOutOfRangeException() {
            var paramMap = new Dictionary<int, MarkingParams> { { 5, new MarkingParams() } };
            var job = CreateTestJob(numLayers: 0, paramMap: paramMap);
            _editor.ApplyParametersToLayerRange(job, 0, 0, 5);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyParametersToLayerRange_StartLayerNegative_ThrowsArgumentOutOfRangeException() {
            var job = CreateTestJob(numLayers: 5, paramMap: new Dictionary<int, MarkingParams> { { 1, new MarkingParams() } });
            _editor.ApplyParametersToLayerRange(job, -1, 3, 1);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyParametersToLayerRange_EndLayerOutOfBounds_ThrowsArgumentOutOfRangeException() {
            var job = CreateTestJob(numLayers: 5, paramMap: new Dictionary<int, MarkingParams> { { 1, new MarkingParams() } });
            _editor.ApplyParametersToLayerRange(job, 2, 5, 1);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyParametersToLayerRange_StartAfterEnd_ThrowsArgumentOutOfRangeException() {
            var job = CreateTestJob(numLayers: 5, paramMap: new Dictionary<int, MarkingParams> { { 1, new MarkingParams() } });
            _editor.ApplyParametersToLayerRange(job, 3, 2, 1);
        }
        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void ApplyParametersToLayerRange_ParamKeyDoesNotExist_ThrowsKeyNotFoundException() {
            var job = CreateTestJob(numLayers: 5, paramMap: new Dictionary<int, MarkingParams> { { 1, new MarkingParams() } });
            _editor.ApplyParametersToLayerRange(job, 0, 4, 99);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyParametersToLayerRange_ValidKeyButInvalidRangeOnEmptyJob_ThrowsArgumentOutOfRangeException() {
            var job = CreateTestJob(numLayers: 0);
            _editor.ApplyParametersToLayerRange(job, 0, 0, 1);
        }
        #endregion
        #region ApplyParametersToVectorTypeInLayer Tests
        [TestMethod]
        public void ApplyParametersToVectorType_TargetsVolume_UpdatesOnlyVolumeBlocks() {
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 7, new MarkingParams() } });
            var plane = job.WorkPlanes[0];
            plane.VectorBlocks.Add(CreateVectorBlock(1, PartArea.Volume));
            plane.VectorBlocks.Add(CreateVectorBlock(2, PartArea.Contour));
            plane.VectorBlocks.Add(CreateVectorBlock(3, PartArea.Volume));
            plane.VectorBlocks.Add(CreateVectorBlock(4, null)); // No metadata
            _editor.ApplyParametersToVectorTypeInLayer(job, 0, PartArea.Volume, 7);
            Assert.AreEqual(7, plane.VectorBlocks[0].MarkingParamsKey, "Volume block should be updated.");
            Assert.AreEqual(2, plane.VectorBlocks[1].MarkingParamsKey, "Contour block should NOT be updated.");
            Assert.AreEqual(7, plane.VectorBlocks[2].MarkingParamsKey, "Second Volume block should be updated.");
            Assert.AreEqual(4, plane.VectorBlocks[3].MarkingParamsKey, "Block with null metadata should NOT be updated.");
        }
        [TestMethod]
        public void ApplyParametersToVectorType_TargetsContour_UpdatesOnlyContourBlocks() {
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 8, new MarkingParams() } });
            var plane = job.WorkPlanes[0];
            plane.VectorBlocks.Add(CreateVectorBlock(1, PartArea.Volume));
            plane.VectorBlocks.Add(CreateVectorBlock(2, PartArea.Contour));
            _editor.ApplyParametersToVectorTypeInLayer(job, 0, PartArea.Contour, 8);
            Assert.AreEqual(1, plane.VectorBlocks[0].MarkingParamsKey, "Volume block should NOT be updated.");
            Assert.AreEqual(8, plane.VectorBlocks[1].MarkingParamsKey, "Contour block should be updated.");
        }
        [TestMethod]
        public void ApplyParametersToVectorType_NoMatchingBlocksExist_MakesNoChanges() {
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 9, new MarkingParams() } });
            var plane = job.WorkPlanes[0];
            plane.VectorBlocks.Add(CreateVectorBlock(1, PartArea.Volume));
            plane.VectorBlocks.Add(CreateVectorBlock(2, PartArea.Volume));
            _editor.ApplyParametersToVectorTypeInLayer(job, 0, PartArea.Contour, 9);
            Assert.AreEqual(1, plane.VectorBlocks[0].MarkingParamsKey, "Block 1 should be unchanged.");
            Assert.AreEqual(2, plane.VectorBlocks[1].MarkingParamsKey, "Block 2 should be unchanged.");
        }
        [TestMethod]
        public void ApplyParametersToVectorType_LayerIsEmpty_DoesNotThrow() {
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 1, new MarkingParams() } });
            // Layer 0 is created with no blocks
            _editor.ApplyParametersToVectorTypeInLayer(job, 0, PartArea.Volume, 1);
            Assert.AreEqual(0, job.WorkPlanes[0].VectorBlocks.Count);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyParametersToVectorType_InvalidLayerIndex_ThrowsArgumentOutOfRangeException() {
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 1, new MarkingParams() } });
            _editor.ApplyParametersToVectorTypeInLayer(job, 5, PartArea.Volume, 1);
        }
        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void ApplyParametersToVectorType_InvalidParamKey_ThrowsKeyNotFoundException() {
            var job = CreateTestJob(numLayers: 1);
            job.WorkPlanes[0].VectorBlocks.Add(CreateVectorBlock(1, PartArea.Volume));
            _editor.ApplyParametersToVectorTypeInLayer(job, 0, PartArea.Volume, 99);
        }
        #endregion
    }
}