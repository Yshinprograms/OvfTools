using OpenVectorFormat;
using OvfParameterModifier.Exceptions;
using OvfParameterModifier.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using PartArea = OpenVectorFormat.VectorBlock.Types.PartArea;

namespace OvfParameterModifier {
    public class ConsoleUI : IUserInterface {
        // REPLACED GetMainMenuSelection with this new dynamic method
        public int DisplayMenuAndGetChoice(List<ICommand> commands) {
            Console.WriteLine("\n--- Main Menu ---");
            for (int i = 0; i < commands.Count; i++) {
                Console.WriteLine($"{i + 1}. {commands[i].Name}");
            }
            Console.WriteLine($"{commands.Count + 1}. Discard All Changes");
            Console.WriteLine($"{commands.Count + 2}. Save and Exit");
            Console.WriteLine($"{commands.Count + 3}. Quit Without Saving");

            int choice = GetIntegerInput("Select an option: ");
            return choice;
        }

        public (int start, int end) GetLayerRange(int maxLayers) {
            Console.WriteLine($"\nEnter the layer range to modify (1-{maxLayers}).");
            int start = GetIntegerInput("  Start Layer: ");
            int end = GetIntegerInput("  End Layer: ");

            if (start > end) {
                throw new UserInputException("The start layer cannot be greater than the end layer.");
            }
            if (start < 1 || end > maxLayers) {
                throw new UserInputException($"Layer range is out of bounds. Must be between 1 and {maxLayers}.");
            }
            return (start - 1, end - 1); // Translate to 0-based
        }

        public int GetTargetLayerIndex(int maxLayers) {
            int layerNum = GetIntegerInput($"\nEnter the Layer number (1-{maxLayers}) you wish to edit: ");
            if (layerNum < 1 || layerNum > maxLayers) {
                throw new UserInputException($"Layer is out of bounds. Must be between 1 and {maxLayers}.");
            }
            return layerNum - 1; // Translate to 0-based
        }

        // --- The rest of the file is mostly the same, just keeping it here for completeness ---

        public PartArea GetPartAreaChoice() {
            Console.WriteLine("\nSelect the vector type to target:");
            Console.WriteLine("  1. Volume (Hatches)");
            Console.WriteLine("  2. Contour");
            Console.Write("Select an option: ");
            string input = Console.ReadLine() ?? "";
            return input switch {
                "1" => PartArea.Volume,
                "2" => PartArea.Contour,
                _ => throw new UserInputException("Invalid selection. Please choose a valid vector type."),
            };
        }

        public string GetNewJobName(string currentName) {
            Console.Write($"\nEnter new job name (current: '{currentName}'): ");
            return Console.ReadLine() ?? "";
        }

        public void DisplayWelcomeMessage() {
            Console.WriteLine("OVF Interactive Parameter Editor");
            Console.WriteLine("==============================");
        }

        public void DisplayGoodbyeMessage() {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nFile saved. Goodbye!");
            Console.ResetColor();
        }

        public ParameterSource GetParameterSourceChoice() {
            Console.WriteLine("\nHow do you want to specify the parameters?");
            Console.WriteLine("  1. Create New (by entering Power and Speed)");
            Console.WriteLine("  2. Use Existing (by entering a Parameter Set ID)");
            Console.WriteLine("  3. Return to Main Menu");
            Console.Write("Select an option: ");
            string input = Console.ReadLine() ?? "";
            return input switch {
                "1" => ParameterSource.CreateNew,
                "2" => ParameterSource.UseExistingId,
                "3" => ParameterSource.ReturnToMenu,
                _ => throw new UserInputException("Invalid selection. Please choose a valid option."),
            };
        }

        public int GetExistingParameterSetId(IEnumerable<int> availableKeys) {
            string keyList = string.Join(", ", availableKeys.OrderBy(k => k));
            Console.WriteLine($"(Available IDs: {keyList})");
            return GetIntegerInput("Enter the ID of the existing Parameter Set to apply: ");
        }

        public void DisplayDashboard(string filePath, string jobName, int layerCount, bool isModified) {
            Console.Clear();
            Console.WriteLine("================================================================");
            Console.WriteLine("OVF Interactive Parameter Editor");
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine($"File:     {filePath}");
            Console.WriteLine($"Job Name: {jobName}");
            Console.WriteLine($"Layers:   {layerCount} (numbered 1 to {layerCount})");
            string status = isModified ? "Modified" : "Unchanged";
            ConsoleColor statusColor = isModified ? ConsoleColor.Yellow : ConsoleColor.Green;
            Console.Write("Status:   ");
            Console.ForegroundColor = statusColor;
            Console.WriteLine(status);
            Console.ResetColor();
            Console.WriteLine("================================================================");
        }

        public string GetSourceFilePath() {
            string? filePath = null;
            while (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                Console.Write("\nEnter path to the SOURCE OVF file to edit: ");
                filePath = Console.ReadLine() ?? "";
                if (!File.Exists(filePath)) {
                    DisplayMessage("ERROR: File not found. Please try again.", isError: true);
                }
            }
            return filePath;
        }

        public string GetOutputFilePath(string defaultPath) {
            Console.Write($"\nEnter path for the new file (or press Enter to use '{defaultPath}'): ");
            string inputPath = Console.ReadLine() ?? "";
            return string.IsNullOrWhiteSpace(inputPath) ? defaultPath : inputPath;
        }

        public void DisplayParameterSets(IDictionary<int, MarkingParams> markingParamsMap) {
            Console.WriteLine("\n--- Existing Parameter Sets ---");
            if (markingParamsMap.Count == 0) {
                Console.WriteLine("No parameter sets found.");
                return;
            }
            foreach (var entry in markingParamsMap.OrderBy(kvp => kvp.Key)) {
                Console.WriteLine($"  ID: {entry.Key}, Name: '{entry.Value.Name}', Power: {entry.Value.LaserPowerInW} W, Speed: {entry.Value.LaserSpeedInMmPerS} mm/s");
            }
        }

        public (float power, float speed) GetDesiredParameters() {
            Console.WriteLine("\nEnter the new parameters to apply.");
            float power = GetFloatInput("  New Laser Power (W): ");
            float speed = GetFloatInput("  New Marking Speed (mm/s): ");
            return (power, speed);
        }

        public (float power, float speed)? GetVectorBlockParametersOrSkip(int planeNum, int blockNum, int totalBlocks, VectorBlock block) {
            Console.WriteLine($"\nEditing Plane {planeNum}, Vector Block {blockNum}/{totalBlocks} (Type: {block.VectorDataCase}, Current Key: {block.MarkingParamsKey})");
            Console.Write("  Enter new Laser Power (W) [Press Enter to skip this block]: ");
            string powerInput = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(powerInput)) {
                Console.WriteLine("  -> Skipped.");
                return null;
            }
            Console.Write("  Enter new Marking Speed (mm/s): ");
            string speedInput = Console.ReadLine() ?? "";
            if (!string.IsNullOrWhiteSpace(speedInput) &&
                float.TryParse(powerInput, out float power) &&
                float.TryParse(speedInput, out float speed)) {
                return (power, speed);
            }
            DisplayMessage("Invalid or incomplete input. Skipping block.", isError: true);
            return null;
        }

        public int GetIntegerInput(string prompt) {
            Console.Write(prompt);
            if (!int.TryParse(Console.ReadLine() ?? "", out int result)) {
                throw new UserInputException("Invalid input. Please enter a valid integer.");
            }
            return result;
        }

        private float GetFloatInput(string prompt) {
            Console.Write(prompt);
            if (!float.TryParse(Console.ReadLine() ?? "", out float result)) {
                throw new UserInputException("Invalid input. Please enter a valid number.");
            }
            return result;
        }

        public void DisplayMessage(string message, bool isError = false) {
            if (isError) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: {message}");
                Console.ResetColor();
            } else {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\nINFO: {message}");
                Console.ResetColor();
            }
        }

        public void WaitForAcknowledgement() {
            Console.WriteLine("\nPress Enter to return to the Main Menu...");
            Console.ReadLine();
        }

        public bool ConfirmQuitWithoutSaving() {
            Console.Write("\nYou have unsaved changes. Are you sure you want to quit? (y/n): ");
            string input = Console.ReadLine()?.ToLower() ?? "";
            return input == "y";
        }

        public bool ConfirmDiscardChanges() {
            Console.Write("\nYou have unsaved changes. Are you sure you want to discard them? This cannot be undone. (y/n): ");
            string input = Console.ReadLine()?.ToLower() ?? "";
            return input == "y";
        }
    }
}