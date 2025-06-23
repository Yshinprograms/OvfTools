// Add these using statements at the top
using Microsoft.Win32;
using OpenVectorFormat; // Needed for the 'Job' class
using OpenVectorFormat.OVFReaderWriter;
using System.Windows;
using System.Windows.Input;

namespace OvfViewer.ViewModels {
    public class MainViewModel : ViewModelBase
    {
        private string _jobName = "No File Loaded";

        public string JobName {
            get => _jobName;
            private set {
                // We check if the new value is actually different from the old one.
                // There's no point in updating the UI if nothing changed.
                if (_jobName != value) {
                    _jobName = value; // Update the private field
                    OnPropertyChanged(); // Raise the notification! The [CallerMemberName] attribute
                                         // will automatically fill in "JobName" for us.
                }
            }
        }

        public ICommand LoadFileCommand { get; }

        public MainViewModel() {
            LoadFileCommand = new RelayCommand(ExecuteLoadFileCommand);
        }

        private void ExecuteLoadFileCommand() {
            var openFileDialog = new OpenFileDialog {
                Filter = "OVF Files (*.ovf)|*.ovf|All files (*.*)|*.*",
                Title = "Select an OVF File"
            };

            if (openFileDialog.ShowDialog() == true) {
                using (var reader = new OVFFileReader()) {
                    try {
                        reader.OpenJob(openFileDialog.FileName);
                        Job loadedJob = reader.CacheJobToMemory();
                        this.JobName = loadedJob.JobMetaData?.JobName ?? "Unnamed Job";
                    } catch (Exception ex) {
                        this.JobName = "Failed to load file!";
                        MessageBox.Show($"Error loading OVF file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}