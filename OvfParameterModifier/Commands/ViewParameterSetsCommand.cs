// In OvfParameterModifier/Commands/ViewParameterSetsCommand.cs

using OpenVectorFormat;
using OvfParameterModifier.Interfaces;

namespace OvfParameterModifier.Commands {
    public class ViewParameterSetsCommand : ICommand
    {
        public string Name => "View existing Parameter Sets";
        public string Description => "Displays a list of all parameter sets currently in the job.";
        public CommandCategory Category => CommandCategory.Viewing;

        public bool Execute(Job job, JobEditor editor, IUserInterface ui) {
            ui.DisplayParameterSets(job.MarkingParamsMap);
            ui.WaitForAcknowledgement();

            return false;
        }
    }
}