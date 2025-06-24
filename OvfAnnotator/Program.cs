// Program.cs
using netDxf;
using OpenVectorFormat.OVFReaderWriter;
using OvfAnnotator; // NEW! We need to tell Program.cs it can use our new class!

// --- Configuration ---
const string ovfInputPath = @"C:\Users\pin20\Downloads\SIMTech_Internship\RTC6_Controller\RTC6_Controller\x64\Debug\valid_3_layers.ovf";
const string dxfOutputPath = @"C:\Users\pin20\Downloads\SIMTech_Internship\RTC6_Controller\RTC6_Controller\x64\Debug\dxfLayer1_3_layers.dxf";
const int layerToProcess = 0;

Console.WriteLine("--- OVF to DXF Annotator (Phase 2: Refactored!) ---");

try {
    // 1. Read the OVF File
    Console.WriteLine($"Reading OVF file: {ovfInputPath}");
    using var reader = new OVFFileReader();
    reader.OpenJob(ovfInputPath);
    var workPlane = reader.GetWorkPlane(layerToProcess);
    Console.WriteLine($"Successfully loaded Layer {layerToProcess} with {workPlane.VectorBlocks.Count} vector blocks.");

    // 2. Delegate the conversion job to our specialist class
    Console.WriteLine("Delegating to the OvfToDxfConverter...");
    var converter = new OvfToDxfConverter();
    DxfDocument dxf = converter.Convert(workPlane);

    // 3. Save the result
    dxf.Save(dxfOutputPath);
    Console.WriteLine($"Successfully saved annotated DXF file to: {dxfOutputPath}");

} catch (Exception ex) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nAn error occurred: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine("\nPress any key to exit.");
Console.ReadKey();