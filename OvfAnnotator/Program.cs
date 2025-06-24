// Program.cs - The Final Version!

using CommandLine;
using OpenVectorFormat.OVFReaderWriter;
using OvfAnnotator;
using System;
using System.IO;

public class Program {
    public static void Main(string[] args) {
        // This is the magic line from the CommandLineParser library.
        // It takes the command-line arguments (`args`), tries to match them
        // to our `Options` class, and then decides what to do.
        Parser.Default.ParseArguments<Options>(args)
               .WithParsed(RunOptionsAndReturnExitCode);
    }

    // This method only runs if the command-line arguments were parsed successfully!
    // It receives a populated `options` object with the user's input.
    public static void RunOptionsAndReturnExitCode(Options options) {
        Console.WriteLine("--- OVF to DXF Annotator ---");
        try {
            // 1. Validate inputs
            if (!File.Exists(options.InputFile)) {
                throw new FileNotFoundException("Input OVF file not found.", options.InputFile);
            }
            // Create the output directory if it doesn't exist
            Directory.CreateDirectory(options.OutputDirectory);

            Console.WriteLine($"Processing file: {options.InputFile}");
            Console.WriteLine($"Output will be saved to: {options.OutputDirectory}");

            // 2. Read the OVF File
            using var reader = new OVFFileReader();
            reader.OpenJob(options.InputFile);
            var jobShell = reader.JobShell;
            int totalLayers = jobShell.NumWorkPlanes;
            Console.WriteLine($"File contains {totalLayers} layers.");

            var converter = new OvfToDxfConverter();

            // 3. THE BIG LOOP! We loop through every layer.
            for (int i = 0; i < totalLayers; i++) {
                Console.WriteLine($"--> Processing Layer {i}...");
                var workPlane = reader.GetWorkPlane(i);

                // Let's create a smart output filename
                string inputFileName = Path.GetFileNameWithoutExtension(options.InputFile);
                string outputFileName = $"{inputFileName}_Layer_{i}.dxf";
                string fullOutputPath = Path.Combine(options.OutputDirectory, outputFileName);

                var dxfDocument = converter.Convert(workPlane);
                dxfDocument.Save(fullOutputPath);
                Console.WriteLine($"    Successfully saved: {outputFileName}");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nProcessing complete! All layers have been converted.");
            Console.ResetColor();
        } catch (Exception ex) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }
}