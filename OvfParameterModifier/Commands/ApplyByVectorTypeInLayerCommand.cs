using OpenVectorFormat;
using OvfParameterModifier.Exceptions;
using OvfParameterModifier.Interfaces;
using System;
using System.Linq;
using PartArea = OpenVectorFormat.VectorBlock.Types.PartArea;

namespace OvfParameterModifier.Commands {
    public class ApplyByVectorTypeInLayerCommand : ICommand {
        public string Name => "Apply Parameters by Vector Type in a Layer";
        public string Description => "Applies a parameter set to only Contour or Volume vectors within a single layer.";

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            try {
                int layerIndex = ui.GetTargetLayerIndex(job.WorkPlanes.Count);
                PartArea targetArea = ui.GetPartAreaChoice();
                int paramKey = GetParameterKey(job, editor, ui);

                editor.ApplyParametersToVectorTypeInLayer(job, layerIndex, targetArea, paramKey);

                ui.DisplayMessage($"Successfully applied Parameter Set ID {paramKey} to {targetArea} vectors in layer {layerIndex + 1}.", isError: false);
                ui.WaitForAcknowledgement();
                return true; // The job was modified
            } catch (UserInputException ex) {
                ui.DisplayMessage(ex.Message, isError: true);
                ui.WaitForAcknowledgement();
                return false;
            } catch (OperationCanceledException) {
                // User chose to return to menu
                return false;
            }
        }

        private int GetParameterKey(Job job, JobEditor editor, IUserInterface ui) {
            var source = ui.GetParameterSourceChoice();
            switch (source) {
                case ParameterSource.CreateNew:
                    (float power, float speed) = ui.GetDesiredParameters();
                    return editor.FindOrCreateParameterSetKey(job, power, speed);

                case ParameterSource.UseExistingId:
                    return ui.GetExistingParameterSetId(job.MarkingParamsMap.Keys);

                case ParameterSource.ReturnToMenu:
                    throw new OperationCanceledException();

                default:
                    throw new UserInputException("Invalid parameter source selected.");
            }
        }
    }
}