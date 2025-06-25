using OpenVectorFormat;
using OvfParameterModifier.Interfaces;

namespace OvfParameterModifier.Commands {
    public class ChangeJobNameCommand : ICommand {
        public string Name => "Change Job Name";
        public string Description => "Edits the name of the job in its metadata.";

        public CommandCategory Category => CommandCategory.Editing;

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            string newName = ui.GetNewJobName(job.JobMetaData?.JobName ?? "N/A");
            if (!string.IsNullOrWhiteSpace(newName)) {
                editor.SetJobName(job, newName);
                return true; // The job was modified
            }
            ui.DisplayMessage("Job name was not changed.", isError: false);
            return false;
        }
    }
}