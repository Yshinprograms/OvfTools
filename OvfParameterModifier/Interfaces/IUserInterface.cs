using OpenVectorFormat;
using System.Collections.Generic;
using PartArea = OpenVectorFormat.VectorBlock.Types.PartArea;

namespace OvfParameterModifier {
    public enum MainMenuOption {
        Unknown = 0,
        ViewParameterSets = 1,
        ApplyToLayerRange = 2,
        ApplyByVectorTypeInLayer = 3,
        EditVectorBlocksInLayer = 4,
        ChangeJobName = 5,
        DiscardChanges = 6,
        SaveAndExit = 7,
        QuitWithoutSaving = 8
    }
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
        MainMenuOption GetMainMenuSelection();
        string GetSourceFilePath();
        string GetOutputFilePath(string defaultPath);
        (int start, int end) GetLayerRange();
        ParameterSource GetParameterSourceChoice();
        int GetExistingParameterSetId(IEnumerable<int> availableKeys);
        (float power, float speed) GetDesiredParameters();
        int GetTargetLayerIndex();
        PartArea GetPartAreaChoice();
        (float power, float speed)? GetVectorBlockParametersOrSkip(int planeNum, int blockNum, int totalBlocks, VectorBlock block);
        void WaitForAcknowledgement();
        bool ConfirmQuitWithoutSaving();
        bool ConfirmDiscardChanges();
        string GetNewJobName(string currentName);
    }
}