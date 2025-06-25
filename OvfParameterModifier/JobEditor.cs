using System;
using System.Globalization;
using OpenVectorFormat;
using PartArea = OpenVectorFormat.VectorBlock.Types.PartArea; // FIX: Added type alias for the nested enum

namespace OvfParameterModifier {
    public class JobEditor {
        public int GetMaxLayerIndex(Job job) {
            return job.WorkPlanes.Count - 1;
        }
        public bool DoesParamSetExist(Job job, int key) {
            return job.MarkingParamsMap.ContainsKey(key);
        }
        public void ApplyParametersToLayerRange(Job job, int startLayer, int endLayer, int paramKey) {
            if (!IsLayerRangeValid(job, startLayer, endLayer)) {
                throw new ArgumentOutOfRangeException(nameof(startLayer), "Layer range is invalid for this job.");
            }
            if (!DoesParamSetExist(job, paramKey)) {
                throw new KeyNotFoundException($"Parameter Set with ID {paramKey} does not exist.");
            }
            for (int i = startLayer; i <= endLayer; i++) {
                var workPlane = job.WorkPlanes[i];
                foreach (var vectorBlock in workPlane.VectorBlocks) {
                    vectorBlock.MarkingParamsKey = paramKey;
                }
            }
        }
        public void ApplyParametersToVectorTypeInLayer(Job job, int layerIndex, PartArea targetArea, int paramKey) {
            if (!IsLayerRangeValid(job, layerIndex, layerIndex)) {
                throw new ArgumentOutOfRangeException(nameof(layerIndex), "Layer index is invalid for this job.");
            }
            if (!DoesParamSetExist(job, paramKey)) {
                throw new KeyNotFoundException($"Parameter Set with ID {paramKey} does not exist.");
            }
            var workPlane = job.WorkPlanes[layerIndex];
            foreach (var vectorBlock in workPlane.VectorBlocks) {
                // Safely check for metadata and matching part area before applying the key
                if (vectorBlock.LpbfMetadata?.PartArea == targetArea) {
                    vectorBlock.MarkingParamsKey = paramKey;
                }
            }
        }
        public int FindOrCreateParameterSetKey(Job job, float power, float speed) {
            var markingParamsMap = job.MarkingParamsMap;
            foreach (var entry in markingParamsMap) {
                if (DoParamsMatch(entry.Value, power, speed)) {
                    return entry.Key;
                }
            }
            var newParams = new MarkingParams {
                LaserPowerInW = power,
                LaserSpeedInMmPerS = speed,
                Name = FormattableString.Invariant($"P{power}W_S{speed}mmps")
            };
            int newKey = GetNextAvailableParamKey(markingParamsMap);
            markingParamsMap.Add(newKey, newParams);
            return newKey;
        }
        public void SetJobName(Job job, string newName) {
            if (job.JobMetaData == null) {
                job.JobMetaData = new Job.Types.JobMetaData();
            }
            job.JobMetaData.JobName = newName;
        }
        private bool IsLayerRangeValid(Job job, int start, int end) {
            if (start > end) return false;
            if (start < 0) return false;
            if (end > GetMaxLayerIndex(job)) return false;
            return true;
        }
        private int GetNextAvailableParamKey(IDictionary<int, MarkingParams> markingParamsMap) {
            if (markingParamsMap.Keys.Count == 0) {
                return 1;
            }
            return markingParamsMap.Keys.Max() + 1;
        }
        private bool DoParamsMatch(MarkingParams existingParams, float desiredPower, float desiredSpeed) {
            bool powerMatches = AreFloatsClose(existingParams.LaserPowerInW, desiredPower);
            bool speedMatches = AreFloatsClose(existingParams.LaserSpeedInMmPerS, desiredSpeed);
            return powerMatches && speedMatches;
        }
        private static bool AreFloatsClose(float a, float b, float tolerance = 0.001f) {
            return Math.Abs(a - b) < tolerance;
        }

        public void ApplyParametersToPart(Job job, int partKey, int paramKey) {
            if (!job.PartsMap.ContainsKey(partKey)) {
                throw new KeyNotFoundException($"Part with ID {partKey} does not exist in the job.");
            }
            if (!DoesParamSetExist(job, paramKey)) {
                throw new KeyNotFoundException($"Parameter Set with ID {paramKey} does not exist.");
            }

            // This is our "search and update" mission!
            // We go through every single block in the entire job.
            foreach (var workPlane in job.WorkPlanes) {
                foreach (var vectorBlock in workPlane.VectorBlocks) {
                    // The '?' is a null-conditional operator. It's a safe way to check this
                    // without causing an error if MetaData happens to be null.
                    if (vectorBlock.MetaData?.PartKey == partKey) {
                        vectorBlock.MarkingParamsKey = paramKey;
                    }
                }
            }
        }
    }
}