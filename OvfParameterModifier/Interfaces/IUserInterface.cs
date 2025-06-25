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

        int DisplayMenuAndGetChoice(List<ICommand> commands);
        void DisplayPartsList(IDictionary<int, Part> partsMap);
        int GetTargetPartId(IEnumerable<int> availableKeys);
        string GetSourceFilePath();
        string GetOutputFilePath(string defaultPath);

        (int start, int end) GetLayerRange(int maxLayers);

        ParameterSource GetParameterSourceChoice();
        int GetExistingParameterSetId(IEnumerable<int> availableKeys);
        (float power, float speed) GetDesiredParameters();

        int GetTargetLayerIndex(int maxLayers);

        PartArea GetPartAreaChoice();
        (float power, float speed)? GetVectorBlockParametersOrSkip(int planeNum, int blockNum, int totalBlocks, VectorBlock block);
        void WaitForAcknowledgement();
        bool ConfirmQuitWithoutSaving();
        bool ConfirmDiscardChanges();
        string GetNewJobName(string currentName);
    }
}