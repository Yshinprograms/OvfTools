using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenVectorFormat;
using OvfParameterModifier;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        private VectorBlock CreateVectorBlock(int initialKey, PartArea? area = null, int? partKey = null) {
            var block = new VectorBlock { MarkingParamsKey = initialKey };
            if (area.HasValue) {
                block.LpbfMetadata = new VectorBlock.Types.LPBFMetadata { PartArea = area.Value };
            }
            if (partKey.HasValue) {
                block.MetaData = new VectorBlock.Types.VectorBlockMetaData { PartKey = partKey.Value };
            }
            return block;
        }
        #endregion
        #region SetJobName Tests [3 Tests]
        [TestMethod]
        public void SetJobName_OnExistingMetaData_UpdatesName() {
            // Arrange
            var job = CreateTestJob();
            job.JobMetaData = new Job.Types.JobMetaData { JobName = "Old Name" };

            // Act
            _editor.SetJobName(job, "New Name");

            // Assert
            Assert.AreEqual("New Name", job.JobMetaData.JobName);
        }

        [TestMethod]
        public void SetJobName_OnJobWithNullMetaData_CreatesMetaDataAndSetsName() {
            // Arrange
            var job = CreateTestJob();
            job.JobMetaData = null; // This is the critical condition we're testing

            // Act
            _editor.SetJobName(job, "First Name");

            // Assert
            Assert.IsNotNull(job.JobMetaData);
            Assert.AreEqual("First Name", job.JobMetaData.JobName);
        }

        [TestMethod]
        public void SetJobName_WithEmptyString_SetsEmptyName() {
            // Arrange
            var job = CreateTestJob();
            job.JobMetaData = new Job.Types.JobMetaData { JobName = "Old Name" };

            // Act
            _editor.SetJobName(job, "");

            // Assert
            Assert.AreEqual("", job.JobMetaData.JobName);
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
        public void FindOrCreateParameterSetKey_CultureInvariantName_UsesDotSeparator() {
            // Arrange
            // Set culture to one that uses a comma for decimals (e.g., German)
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var job = CreateTestJob();

            // Act
            int key = _editor.FindOrCreateParameterSetKey(job, 123.45f, 67.8f);
            var newParam = job.MarkingParamsMap[key];

            // Assert
            // We assert that the name uses '.' regardless of the thread's culture
            Assert.AreEqual("P123.45W_S67.8mmps", newParam.Name);
        }
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
        #region ApplyParametersToLayerRange Tests (10 Tests)
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
        #region ApplyParametersToVectorType Tests (8 Tests)
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
        [TestMethod]
        public void ApplyParametersToVectorType_TargetsTransitionContour_UpdatesOnlyTransitionContourBlocks() {
            // Arrange
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 7, new MarkingParams() } });
            var plane = job.WorkPlanes[0];
            plane.VectorBlocks.Add(CreateVectorBlock(1, PartArea.Volume));
            plane.VectorBlocks.Add(CreateVectorBlock(2, PartArea.Contour));
            plane.VectorBlocks.Add(CreateVectorBlock(3, PartArea.TransitionContour)); // Target this one

            // Act
            _editor.ApplyParametersToVectorTypeInLayer(job, 0, PartArea.TransitionContour, 7);

            // Assert
            Assert.AreEqual(1, plane.VectorBlocks[0].MarkingParamsKey, "Volume block should NOT be updated.");
            Assert.AreEqual(2, plane.VectorBlocks[1].MarkingParamsKey, "Contour block should NOT be updated.");
            Assert.AreEqual(7, plane.VectorBlocks[2].MarkingParamsKey, "TransitionContour block should be updated.");
        }

        [TestMethod]
        public void ApplyParametersToVectorType_SkipsBlocksWithNullLpbfMetadata() {
            // Arrange
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 5, new MarkingParams() } });
            var plane = job.WorkPlanes[0];
            var blockWithMetadata = CreateVectorBlock(1, PartArea.Volume);
            var blockWithoutMetadata = new VectorBlock { MarkingParamsKey = 2 }; // No LPBFMetadata at all
            plane.VectorBlocks.Add(blockWithMetadata);
            plane.VectorBlocks.Add(blockWithoutMetadata);

            // Act
            _editor.ApplyParametersToVectorTypeInLayer(job, 0, PartArea.Volume, 5);

            // Assert
            Assert.AreEqual(5, plane.VectorBlocks[0].MarkingParamsKey, "Block with matching metadata should be updated.");
            Assert.AreEqual(2, plane.VectorBlocks[1].MarkingParamsKey, "Block with null metadata should be SKIPPED.");
        }
        #endregion
        #region ApplyParametersToPart Tests (5 Tests)

        [TestMethod]
        public void ApplyParametersToPart_HappyPath_UpdatesCorrectBlocksOnly() {
            // --- ARRANGE ---
            var paramMap = new Dictionary<int, MarkingParams> { { 99, new MarkingParams() } };
            var job = CreateTestJob(numLayers: 2, paramMap: paramMap);

            // Add parts to the job's manifest
            job.PartsMap.Add(1, new Part { Name = "Part 1" });
            job.PartsMap.Add(2, new Part { Name = "Part 2" });

            // Layer 0
            job.WorkPlanes[0].VectorBlocks.Add(CreateVectorBlock(initialKey: 10, partKey: 1)); // Should not change
            job.WorkPlanes[0].VectorBlocks.Add(CreateVectorBlock(initialKey: 20, partKey: 2)); // Should change
            // Layer 1
            job.WorkPlanes[1].VectorBlocks.Add(CreateVectorBlock(initialKey: 30, partKey: 2)); // Should change
            job.WorkPlanes[1].VectorBlocks.Add(CreateVectorBlock(initialKey: 40, partKey: 1)); // Should not change
            job.WorkPlanes[1].VectorBlocks.Add(CreateVectorBlock(initialKey: 50)); // No part key, should not change

            // --- ACT ---
            _editor.ApplyParametersToPart(job, partKey: 2, paramKey: 99);

            // --- ASSERT ---
            // Blocks for Part 1 are UNCHANGED
            Assert.AreEqual(10, job.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey);
            Assert.AreEqual(40, job.WorkPlanes[1].VectorBlocks[1].MarkingParamsKey);
            // Blocks for Part 2 ARE UPDATED to 99
            Assert.AreEqual(99, job.WorkPlanes[0].VectorBlocks[1].MarkingParamsKey);
            Assert.AreEqual(99, job.WorkPlanes[1].VectorBlocks[0].MarkingParamsKey);
            // Block with no part key is UNCHANGED
            Assert.AreEqual(50, job.WorkPlanes[1].VectorBlocks[2].MarkingParamsKey);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void ApplyParametersToPart_InvalidPartKey_ThrowsKeyNotFoundException() {
            // --- ARRANGE ---
            var job = CreateTestJob(numLayers: 1);
            job.PartsMap.Add(1, new Part()); // Only Part 1 exists
            job.MarkingParamsMap.Add(99, new MarkingParams());

            // --- ACT ---
            _editor.ApplyParametersToPart(job, partKey: 5, paramKey: 99); // Try to edit Part 5
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void ApplyParametersToPart_InvalidParamKey_ThrowsKeyNotFoundException() {
            // --- ARRANGE ---
            var job = CreateTestJob(numLayers: 1);
            job.PartsMap.Add(1, new Part());
            job.MarkingParamsMap.Add(99, new MarkingParams()); // Only Param 99 exists

            // --- ACT ---
            _editor.ApplyParametersToPart(job, partKey: 1, paramKey: 101); // Try to apply Param 101
        }

        [TestMethod]
        public void ApplyParametersToPart_PartExistsButHasNoBlocks_DoesNotThrowAndMakesNoChanges() {
            // --- ARRANGE ---
            var paramMap = new Dictionary<int, MarkingParams> { { 99, new MarkingParams() } };
            var job = CreateTestJob(numLayers: 1, paramMap: paramMap);
            job.PartsMap.Add(1, new Part { Name = "Part 1" });
            job.PartsMap.Add(2, new Part { Name = "Part 2" }); // Part 2 exists but no blocks are assigned to it
            job.WorkPlanes[0].VectorBlocks.Add(CreateVectorBlock(initialKey: 10, partKey: 1));

            // --- ACT ---
            // This should execute without error
            _editor.ApplyParametersToPart(job, partKey: 2, paramKey: 99);

            // --- ASSERT ---
            // Nothing should have changed
            Assert.AreEqual(10, job.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey);
        }
        [TestMethod]
        public void ApplyParametersToPart_SkipsBlocksWithNullMetaData() {
            // Arrange
            var job = CreateTestJob(numLayers: 1, paramMap: new Dictionary<int, MarkingParams> { { 99, new MarkingParams() } });
            job.PartsMap.Add(1, new Part());
            var blockWithPartKey = CreateVectorBlock(10, partKey: 1);
            var blockWithNullMetaData = new VectorBlock { MarkingParamsKey = 20 }; // MetaData property itself is null
            job.WorkPlanes[0].VectorBlocks.Add(blockWithPartKey);
            job.WorkPlanes[0].VectorBlocks.Add(blockWithNullMetaData);

            // Act
            _editor.ApplyParametersToPart(job, partKey: 1, paramKey: 99);

            // Assert
            Assert.AreEqual(99, job.WorkPlanes[0].VectorBlocks[0].MarkingParamsKey, "Block with part key should be updated.");
            Assert.AreEqual(20, job.WorkPlanes[0].VectorBlocks[1].MarkingParamsKey, "Block with null metadata should be SKIPPED.");
        }
        #endregion
    }
}