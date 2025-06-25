using OpenVectorFormat;
using OvfParameterModifier.Interfaces;
using OvfParameterModifier.Services;
using System.Linq;

namespace OvfParameterModifier.Commands {
    public class AssignPartsCommand : ICommand {
        public string Name => "Automatic Part Assignment";
        public string Description => "Analyzes layer contours & compares " +
            "between consecutive layers to identify and assign 3D parts.";
        public CommandCategory Category => CommandCategory.Processing;

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            ui.DisplayMessage("Analyzing contours and assigning parts... this may take a moment.", isError: false);

            var assigner = new PartAssignerService();

            assigner.AssignParts(job);

            int partsFound = job.PartsMap.Keys.Count;
            ui.DisplayMessage($"Success! Found and assigned {partsFound} unique parts.", isError: false);
            ui.WaitForAcknowledgement();

            return true;
        }
    }
}