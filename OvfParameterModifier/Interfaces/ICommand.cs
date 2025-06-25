// In OvfParameterModifier/Interfaces/ICommand.cs

using OpenVectorFormat;

namespace OvfParameterModifier.Interfaces {
    public enum CommandCategory {
        Viewing,
        Processing,
        Editing
    }
    public interface ICommand {
        /// <summary>
        /// The name of the command to be displayed in the main menu.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The description of the command to be displayed in the main menu.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes the command's primary logic.
        /// </summary>
        /// <param name="job">The current Job object being edited.</param>
        /// <param name="editor">The JobEditor service for core logic.</param>
        /// <param name="ui">The user interface for interaction.</param>
        /// <returns>True if the job was modified; otherwise, false.</returns>
        bool Execute(Job job, JobEditor editor, IUserInterface ui);

        CommandCategory Category { get; }
    }
}