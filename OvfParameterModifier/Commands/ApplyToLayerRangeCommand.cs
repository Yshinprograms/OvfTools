using OpenVectorFormat;
using OvfParameterModifier.Exceptions;
using OvfParameterModifier.Interfaces;
using System;
using System.Linq;

namespace OvfParameterModifier.Commands {
    public class ApplyToLayerRangeCommand : ICommand {
        public string Name => "Apply Parameters to a Layer Range";
        public string Description => "Applies a single parameter set to all vector blocks within a specified range of layers.";

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            try {
                (int startLayer, int endLayer) = ui.GetLayerRange(job.WorkPlanes.Count);
                int paramKey = GetParameterKey(job, editor, ui);

                editor.ApplyParametersToLayerRange(job, startLayer, endLayer, paramKey);

                ui.DisplayMessage($"Successfully applied Parameter Set ID {paramKey} to layers {startLayer + 1} through {endLayer + 1}.", isError: false);
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