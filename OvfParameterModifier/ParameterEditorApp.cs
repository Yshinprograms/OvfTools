using OpenVectorFormat;
using OpenVectorFormat.OVFReaderWriter;
using OvfParameterModifier.Commands;
using OvfParameterModifier.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace OvfParameterModifier {
    public class ParameterEditorApp {
        private readonly IUserInterface _ui;
        private readonly JobEditor _editor;
        private string _sourceFilePath;
        private Job _job;
        private bool _isModified = false;

        private readonly List<ICommand> _commands;

        public ParameterEditorApp(IUserInterface ui, JobEditor editor) {
            _ui = ui;
            _editor = editor;

            // Initialize all our commands. Adding a new one is as simple as adding a line here!
            var unsortedCommands = new List<ICommand>
            {
                new ViewParameterSetsCommand(),
                new ApplyToLayerRangeCommand(),
                new ApplyByVectorTypeInLayerCommand(),
                new EditVectorBlocksCommand(),
                new ChangeJobNameCommand(),
                new AssignPartsCommand(),
                new ApplyParametersToPartCommand(),
                // Add future commands here!
            };

            _commands = unsortedCommands
                    .OrderBy(c => (int)c.Category)
                    .ThenBy(c => c.Name)
                    .ToList();
        }

        public void Run() {
            _ui.DisplayWelcomeMessage();
            if (!LoadInitialFile()) return;
            MainMenuLoop();
        }

        private bool LoadInitialFile() {
            try {
                _sourceFilePath = _ui.GetSourceFilePath();
                _job = LoadJobFromFile(_sourceFilePath);
                _isModified = false;
                return true;
            } catch (Exception ex) {
                _ui.DisplayMessage($"Failed to load job: {ex.Message}", isError: true);
                return false;
            }
        }

        private void MainMenuLoop() {
            while (true) {
                _ui.DisplayDashboard(_sourceFilePath, _job.JobMetaData?.JobName, _job.WorkPlanes.Count, _isModified);
                int choice = _ui.DisplayMenuAndGetChoice(_commands);

                if (choice > 0 && choice <= _commands.Count) {
                    var command = _commands[choice - 1];
                    bool wasModifiedByCommand = command.Execute(_job, _editor, _ui);
                    if (wasModifiedByCommand) {
                        _isModified = true;
                    }
                    continue;
                }

                int metaOptionBaseIndex = _commands.Count + 1;

                if (choice == metaOptionBaseIndex) // Handle Help
                {
                    _ui.DisplayHelp(_commands);
                    _ui.WaitForAcknowledgement();
                } else if (choice == metaOptionBaseIndex + 1) // Handle Discard
                  {
                    HandleDiscardChanges();
                } else if (choice == metaOptionBaseIndex + 2) // Handle Save
                  {
                    HandleSaveAndExit();
                    return;
                } else if (choice == metaOptionBaseIndex + 3) // Handle Quit
                  {
                    if (HandleQuit()) {
                        return;
                    }
                } else {
                    _ui.DisplayMessage("Invalid option selected. Please try again.", isError: true);
                    _ui.WaitForAcknowledgement();
                }
            }
        }

        private void HandleSaveAndExit() {
            string defaultPath = Path.ChangeExtension(_sourceFilePath, ".modified.ovf");
            string outputPath = _ui.GetOutputFilePath(defaultPath);

            try {
                _ui.DisplayMessage($"Saving to {outputPath}...", isError: false);
                using (var writer = new OVFFileWriter()) {
                    // Step 1: Initialize the file with the Job Shell
                    writer.StartWritePartial(_job, outputPath);

                    // Step 2: Append each WorkPlane one by one
                    foreach (var workPlane in _job.WorkPlanes) {
                        writer.AppendWorkPlane(workPlane);
                    }

                    // Step 3: Dispose() finalizes the file by writing the last plane,
                    // the final job shell, and the LUT. THIS IS CRUCIAL.
                }
                _ui.DisplayGoodbyeMessage();
            } catch (Exception ex) {
                _ui.DisplayMessage($"Failed to save file: {ex.Message}", isError: true);
                _ui.WaitForAcknowledgement();
            }
        }

        private void HandleDiscardChanges() {
            if (_isModified && _ui.ConfirmDiscardChanges()) {
                try {
                    _job = LoadJobFromFile(_sourceFilePath);
                    _isModified = false;
                    _ui.DisplayMessage("All changes have been discarded.", isError: false);
                } catch (Exception ex) {
                    _ui.DisplayMessage($"Failed to reload original file: {ex.Message}", isError: true);
                }
            } else if (!_isModified) {
                _ui.DisplayMessage("There are no changes to discard.", isError: false);
            }
            _ui.WaitForAcknowledgement();
        }

        private bool HandleQuit() {
            if (_isModified) {
                return _ui.ConfirmQuitWithoutSaving();
            }
            return true; 
        }

        private Job LoadJobFromFile(string path) {
            _ui.DisplayMessage($"Loading job from {path}...", isError: false);
            using var reader = new OVFFileReader();
            // OpenJob loads the shell, CacheJobToMemory loads the full data
            reader.OpenJob(path);
            var job = reader.CacheJobToMemory();
            _ui.DisplayMessage("Job loaded successfully!", isError: false);
            return job;
        }
    }
}