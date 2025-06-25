using OpenVectorFormat;
using OvfParameterModifier.Exceptions;
using OvfParameterModifier.Interfaces;
using System;
using System.Linq;

namespace OvfParameterModifier.Commands {
    public class ApplyParametersToPartCommand : ICommand {
        public string Name => "Apply Parameters by Part";
        public string Description => "Applies a parameter set to all vector blocks associated with a specific part.";

        public CommandCategory Category => CommandCategory.Editing;

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            if (job.PartsMap.Count == 0) {
                ui.DisplayMessage("No parts found. Please run the 'Assign Parts from Contours' command first.", isError: true);
                ui.WaitForAcknowledgement();
                return false;
            }

            try {
                ui.DisplayPartsList(job.PartsMap);
                int partKey = ui.GetTargetPartId(job.PartsMap.Keys);

                int paramKey = GetParameterKey(job, editor, ui);

                editor.ApplyParametersToPart(job, partKey, paramKey);

                ui.DisplayMessage($"Successfully applied Parameter Set ID {paramKey} to Part ID {partKey}.", isError: false);
                ui.WaitForAcknowledgement();
                return true; // The job was absolutely modified
            } catch (UserInputException ex) {
                ui.DisplayMessage(ex.Message, isError: true);
                ui.WaitForAcknowledgement();
                return false;
            } catch (OperationCanceledException) {
                return false; // User chose to return to the menu
            } catch (KeyNotFoundException ex) {
                ui.DisplayMessage(ex.Message, isError: true);
                ui.WaitForAcknowledgement();
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