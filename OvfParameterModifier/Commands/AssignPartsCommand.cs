using OpenVectorFormat;
using OvfParameterModifier.Interfaces;
using OvfParameterModifier.Services;
using System.Linq;

namespace OvfParameterModifier.Commands {
    public class AssignPartsCommand : ICommand {
        public string Name => "Assign Parts from Contours";
        public string Description => "Analyzes layer contours to identify and assign 3D parts.";

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            ui.DisplayMessage("Analyzing contours and assigning parts... this may take a moment.", isError: false);

            // 1. Create an instance of our powerful engine.
            var assigner = new PartAssignerService();

            // 2. Tell the engine to do its magic on our job.
            assigner.AssignParts(job);

            // 3. Report the wonderful news back to the user!
            int partsFound = job.PartsMap.Keys.Count;
            ui.DisplayMessage($"Success! Found and assigned {partsFound} unique parts.", isError: false);
            ui.WaitForAcknowledgement();

            // 4. Tell the main app that the job has been changed!
            return true;
        }
    }
}