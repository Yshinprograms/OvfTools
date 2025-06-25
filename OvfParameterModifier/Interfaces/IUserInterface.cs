using OpenVectorFormat;
using System.Collections.Generic;
using PartArea = OpenVectorFormat.VectorBlock.Types.PartArea;

namespace OvfParameterModifier.Interfaces {
    public enum ParameterSource {
        CreateNew,
        UseExistingId,
        ReturnToMenu
    }

    public interface IUserInterface {
        void DisplayWelcomeMessage();
        void DisplayGoodbyeMessage();
        void DisplayMessage(string message, bool isError = false);
        void DisplayParameterSets(IDictionary<int, MarkingParams> markingParamsMap);
        void DisplayDashboard(string filePath, string jobName, int layerCount, bool isModified);

        // REVISED: This now takes a list of commands and returns a choice
        int DisplayMenuAndGetChoice(List<ICommand> commands);

        string GetSourceFilePath();
        string GetOutputFilePath(string defaultPath);

        // REVISED: Added maxLayers parameter for validation
        (int start, int end) GetLayerRange(int maxLayers);

        ParameterSource GetParameterSourceChoice();
        int GetExistingParameterSetId(IEnumerable<int> availableKeys);
        (float power, float speed) GetDesiredParameters();

        // REVISED: Added maxLayers parameter for validation
        int GetTargetLayerIndex(int maxLayers);

        PartArea GetPartAreaChoice();
        (float power, float speed)? GetVectorBlockParametersOrSkip(int planeNum, int blockNum, int totalBlocks, VectorBlock block);
        void WaitForAcknowledgement();
        bool ConfirmQuitWithoutSaving();
        bool ConfirmDiscardChanges();
        string GetNewJobName(string currentName);
    }
}