using CommandLine;

namespace OvfAnnotator {
    public class Options {
        [Option('i', "input", Required = true, HelpText = "Input OVF file to process.")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = false, HelpText = "Optional: Output directory. If not specified, a folder will be created next to the input file.")]
        public string OutputDirectory { get; set; }

        [Option('l', "layers", Required = false, HelpText = "Optional: Specify a 1-based layer range to process (e.g., \"1-20\" or \"5\"). Processes all layers if omitted.")]
        public string LayerRange { get; set; }

        [Option('t', "textHeight", Required = false, Default = 1.0, HelpText = "Optional: Sets the text height for the block ID annotations in the DXF file.")]
        public double TextHeight { get; set; }

        [Option('s', "simpleId", Required = false, Default = false, HelpText = "Optional: If present, displays the block annotation as a simple number (e.g., '12') instead of 'ID: 12'.")]
        public bool SimpleId { get; set; }

        [Option('b', "by-block", Required = false, Default = false, HelpText = "Optional: When present, colors each vector block individually instead of by Part ID.")]
        public bool ColorByBlock { get; set; }
    }
}