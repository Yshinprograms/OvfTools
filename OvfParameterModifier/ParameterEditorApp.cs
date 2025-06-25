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

        // The new, clean list of available commands
        private readonly List<ICommand> _commands;

        public ParameterEditorApp(IUserInterface ui, JobEditor editor) {
            _ui = ui;
            _editor = editor;

            // Initialize all our commands. Adding a new one is as simple as adding a line here!
            _commands = new List<ICommand>
            {
                new ViewParameterSetsCommand(),
                new ApplyToLayerRangeCommand(),
                new ApplyByVectorTypeInLayerCommand(),
                new EditVectorBlocksCommand(),
                new ChangeJobNameCommand(),
                // Add future commands like "new AssignPartsCommand()" here!
            };
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

                // Handle commands
                if (choice > 0 && choice <= _commands.Count) {
                    var command = _commands[choice - 1];
                    bool wasModifiedByCommand = command.Execute(_job, _editor, _ui);
                    if (wasModifiedByCommand) {
                        _isModified = true;
                    }
                    continue;
                }

                // Handle meta-options (not commands)
                int metaOption = choice - _commands.Count;
                switch (metaOption) {
                    case 1: // Discard Changes
                        HandleDiscardChanges();
                        break;
                    case 2: // Save and Exit
                        HandleSaveAndExit();
                        return; // Exit loop and application
                    case 3: // Quit Without Saving
                        if (HandleQuit()) {
                            return; // Exit loop and application
                        }
                        break;
                    default:
                        _ui.DisplayMessage("Invalid option selected. Please try again.", isError: true);
                        _ui.WaitForAcknowledgement();
                        break;
                }
            }
        }

        private void HandleSaveAndExit() {
            string defaultPath = Path.ChangeExtension(_sourceFilePath, ".modified.ovf");
            string outputPath = _ui.GetOutputFilePath(defaultPath);

            try {
                using (var writer = new OVFFileWriter()) {
                    // Using SimpleJobWrite which is a robust wrapper around the partial write logic
                    writer.SimpleJobWrite(_job, outputPath);
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
            return true; // No changes, quit freely
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