// Options.cs

using CommandLine;

namespace OvfAnnotator {
    public class Options {
        [Option('i', "input", Required = true, HelpText = "Input OVF file to process.")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = false, HelpText = "Optional: Output directory. If not specified, a folder will be created next to the input file.")]
        public string OutputDirectory { get; set; }

        // NEW! Our powerful layer selection option.
        [Option('l', "layers", Required = false, HelpText = "Optional: Specify a layer range to process (e.g., \"10-20\" or \"5\"). Processes all layers if omitted.")]
        public string LayerRange { get; set; }
    }
}