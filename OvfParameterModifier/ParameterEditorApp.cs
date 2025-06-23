using OpenVectorFormat;
using OpenVectorFormat.OVFReaderWriter;
using OvfParameterModifier.Exceptions;
using PartArea = OpenVectorFormat.VectorBlock.Types.PartArea; // FIX: Added type alias for the nested enum

namespace OvfParameterModifier {
    public class ParameterEditorApp(IUserInterface ui, JobEditor editor) {
        private Job _activeJob = null!;
        private string _sourceFilePath = null!;
        private bool _isModified = false;
        public void Run() {
            ui.DisplayWelcomeMessage();
            try {
                LoadJob();
                MainLoop();
            } catch (Exception ex) {
                ui.DisplayMessage($"A fatal error occurred: {ex.Message}", isError: true);
            }
        }
        private void MainLoop() {
            bool running = true;
            while (running) {
                ui.DisplayDashboard(_sourceFilePath, _activeJob.JobMetaData.JobName, _activeJob.WorkPlanes.Count, _isModified);
                try {
                    var choice = ui.GetMainMenuSelection();
                    switch (choice) {
                        case MainMenuOption.ViewParameterSets:
                            ui.DisplayParameterSets(_activeJob.MarkingParamsMap);
                            ui.WaitForAcknowledgement();
                            break;
                        case MainMenuOption.ApplyToLayerRange:
                            DoApplyParametersToRange();
                            break;
                        case MainMenuOption.ApplyByVectorTypeInLayer:
                            DoApplyByVectorType();
                            break;
                        case MainMenuOption.EditVectorBlocksInLayer:
                            DoVectorBlockEditing();
                            break;
                        case MainMenuOption.ChangeJobName:
                            DoChangeJobName();
                            break;
                        case MainMenuOption.DiscardChanges:
                            DoDiscardChanges();
                            break;
                        case MainMenuOption.SaveAndExit:
                            DoSaveAndExit();
                            running = false;
                            break;
                        case MainMenuOption.QuitWithoutSaving:
                            running = !DoQuitWithoutSaving();
                            break;
                        default:
                            ui.DisplayMessage("Invalid selection, please try again.", isError: true);
                            ui.WaitForAcknowledgement();
                            break;
                    }
                } catch (UserInputException ex) {
                    ui.DisplayMessage(ex.Message, isError: true);
                    ui.WaitForAcknowledgement();
                } catch (Exception ex) {
                    ui.DisplayMessage(ex.Message, isError: true);
                    ui.WaitForAcknowledgement();
                }
            }
        }
        private void DoChangeJobName() {
            string currentName = _activeJob.JobMetaData?.JobName ?? "Unnamed Job";
            string newName = ui.GetNewJobName(currentName);
            if (string.IsNullOrWhiteSpace(newName)) {
                ui.DisplayMessage("Job name cannot be empty. No change was made.", isError: true);
                ui.WaitForAcknowledgement();
                return;
            }
            editor.SetJobName(_activeJob, newName);
            _isModified = true;
            ui.DisplayMessage("Job name updated successfully.");
            ui.WaitForAcknowledgement();
        }
        private int? GetParameterKeyFromUser() {
            var choice = ui.GetParameterSourceChoice();
            if (choice == ParameterSource.ReturnToMenu) {
                return null;
            }
            int keyToUse;
            if (choice == ParameterSource.UseExistingId) {
                keyToUse = ui.GetExistingParameterSetId(_activeJob.MarkingParamsMap.Keys);
                if (!editor.DoesParamSetExist(_activeJob, keyToUse)) {
                    throw new OvfParameterModifierException($"Parameter Set with ID {keyToUse} does not exist.");
                }
            } else {
                var (power, speed) = ui.GetDesiredParameters();
                keyToUse = editor.FindOrCreateParameterSetKey(_activeJob, power, speed);
            }
            return keyToUse;
        }
        private void DoApplyParametersToRange() {
            int? keyToUse = GetParameterKeyFromUser();
            if (!keyToUse.HasValue) return;

            var (startLayer, endLayer) = ui.GetLayerRange();
            int maxLayer = editor.GetMaxLayerIndex(_activeJob) + 1;
            if (startLayer < 1 || endLayer > maxLayer || startLayer > endLayer) {
                ui.DisplayMessage($"Invalid layer range. Please enter numbers between 1 and {maxLayer}.", isError: true);
                ui.WaitForAcknowledgement();
                return;
            }
            editor.ApplyParametersToLayerRange(_activeJob, startLayer - 1, endLayer - 1, keyToUse.Value);
            _isModified = true;
            ui.DisplayMessage($"Successfully applied Parameter Set ID {keyToUse} to layers {startLayer}-{endLayer}.");
            ui.WaitForAcknowledgement();
        }
        private void DoApplyByVectorType() {
            int maxLayer = editor.GetMaxLayerIndex(_activeJob) + 1;
            if (maxLayer <= 0) {
                ui.DisplayMessage("This job has no layers to edit.", isError: true);
                ui.WaitForAcknowledgement();
                return;
            }
            int layerNumber = ui.GetTargetLayerIndex();
            if (layerNumber < 1 || layerNumber > maxLayer) {
                ui.DisplayMessage($"Invalid layer number. Please enter a number between 1 and {maxLayer}.", isError: true);
                ui.WaitForAcknowledgement();
                return;
            }
            PartArea targetArea = ui.GetPartAreaChoice();
            int? keyToUse = GetParameterKeyFromUser();
            if (!keyToUse.HasValue) return;

            editor.ApplyParametersToVectorTypeInLayer(_activeJob, layerNumber - 1, targetArea, keyToUse.Value);
            _isModified = true;
            ui.DisplayMessage($"Successfully applied Parameter Set ID {keyToUse} to all '{targetArea}' vectors in layer {layerNumber}.");
            ui.WaitForAcknowledgement();
        }
        private void DoDiscardChanges() {
            if (!_isModified) {
                ui.DisplayMessage("No changes to discard.");
                ui.WaitForAcknowledgement();
                return;
            }
            if (ui.ConfirmDiscardChanges()) {
                try {
                    ReloadActiveJob();
                    _isModified = false;
                    ui.DisplayMessage("All changes have been discarded.");
                    ui.WaitForAcknowledgement();
                } catch (Exception ex) {
                    ui.DisplayMessage($"Failed to reload file: {ex.Message}", isError: true);
                    ui.WaitForAcknowledgement();
                }
            }
        }
        private bool DoQuitWithoutSaving() {
            if (_isModified) {
                if (ui.ConfirmQuitWithoutSaving()) {
                    ui.DisplayMessage("Exiting without saving changes. Goodbye!");
                    return true;
                }
                return false;
            }
            ui.DisplayMessage("Exiting application. Goodbye!");
            return true;
        }
        private void DoVectorBlockEditing() {
            int layerNumber = ui.GetTargetLayerIndex();
            int maxLayer = editor.GetMaxLayerIndex(_activeJob) + 1;
            if (layerNumber < 1 || layerNumber > maxLayer) {
                ui.DisplayMessage($"Invalid layer number. Please enter a number between 1 and {maxLayer}.", isError: true);
                ui.WaitForAcknowledgement();
                return;
            }
            int layerIndex = layerNumber - 1;
            var workPlane = _activeJob.WorkPlanes[layerIndex];
            bool wasAnyBlockModified = false;
            for (int i = 0; i < workPlane.VectorBlocks.Count; i++) {
                var block = workPlane.VectorBlocks[i];
                var desiredParams = ui.GetVectorBlockParametersOrSkip(layerNumber, i + 1, workPlane.VectorBlocks.Count, block);
                if (desiredParams.HasValue) {
                    var (power, speed) = desiredParams.Value;
                    int keyToUse = editor.FindOrCreateParameterSetKey(_activeJob, power, speed);
                    block.MarkingParamsKey = keyToUse;
                    wasAnyBlockModified = true;
                }
            }
            if (wasAnyBlockModified) {
                _isModified = true;
                ui.DisplayMessage($"Changes applied successfully to Layer {layerNumber}.");
            } else {
                ui.DisplayMessage($"No changes were made to Layer {layerNumber}.");
            }
            ui.WaitForAcknowledgement();
        }
        private void LoadJob() {
            _sourceFilePath = ui.GetSourceFilePath();
            ReloadActiveJob();
            ui.DisplayMessage($"Successfully loaded '{_activeJob.JobMetaData.JobName}' with {_activeJob.WorkPlanes.Count} work planes.");
            ui.WaitForAcknowledgement();
        }
        private void ReloadActiveJob() {
            using (var reader = new OVFFileReader()) {
                reader.OpenJob(_sourceFilePath);
                _activeJob = reader.CacheJobToMemory();
            }
        }
        private void DoSaveAndExit() {
            string defaultPath = Path.ChangeExtension(_sourceFilePath, ".modified.ovf");
            string outputPath = ui.GetOutputFilePath(defaultPath);
            using (var writer = new OVFFileWriter()) {
                writer.StartWritePartial(_activeJob, outputPath);
                foreach (var workPlane in _activeJob.WorkPlanes) {
                    writer.AppendWorkPlane(workPlane);
                }
            }
            ui.DisplayGoodbyeMessage();
        }
    }
}