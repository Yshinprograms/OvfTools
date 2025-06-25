using OpenVectorFormat;
using OvfParameterModifier.Exceptions;
using OvfParameterModifier.Interfaces;
using System;

namespace OvfParameterModifier.Commands {
    public class EditVectorBlocksCommand : ICommand {
        public string Name => "Edit Individual Vector Blocks in a Layer";
        public string Description => "Manually step through and edit each vector block in a single layer (Proof of Concept).";

        public CommandCategory Category => CommandCategory.Editing;

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            try {
                int layerIndex = ui.GetTargetLayerIndex(job.WorkPlanes.Count);
                var workPlane = job.WorkPlanes[layerIndex];
                int totalBlocks = workPlane.VectorBlocks.Count;
                bool wasModified = false;

                for (int i = 0; i < totalBlocks; i++) {
                    var block = workPlane.VectorBlocks[i];
                    var newParams = ui.GetVectorBlockParametersOrSkip(layerIndex + 1, i + 1, totalBlocks, block);

                    if (newParams.HasValue) {
                        int newKey = editor.FindOrCreateParameterSetKey(job, newParams.Value.power, newParams.Value.speed);
                        block.MarkingParamsKey = newKey;
                        wasModified = true;
                    }
                }
                return wasModified;
            } catch (UserInputException ex) {
                ui.DisplayMessage(ex.Message, isError: true);
                ui.WaitForAcknowledgement();
                return false;
            }
        }
    }
}