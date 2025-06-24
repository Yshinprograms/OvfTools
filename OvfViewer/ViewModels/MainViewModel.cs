// Add these using statements at the top
using Microsoft.Win32;
using OpenVectorFormat;
using OpenVectorFormat.OVFReaderWriter;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace OvfViewer.ViewModels {
    public class MainViewModel : ViewModelBase
    {
        private string _jobName = "No File Loaded";
        private Job? _loadedJob;
        private string? _selectedLayerSummary;

        public Job? LoadedJob
        {
            get => _loadedJob;
            private set
            {
                if (_loadedJob != value)
                {
                    _loadedJob = value;
                    OnPropertyChanged();
                    UpdateLayerSummaries();
                }
            }
        }

        public string JobName {
            get => _jobName;
            private set {
                if (_jobName != value) {
                    _jobName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> LayerSummaries { get; } = new();

        public string? SelectedLayerSummary
        {
            get => _selectedLayerSummary;
            set
            {
                if (_selectedLayerSummary != value)
                {
                    _selectedLayerSummary = value;
                    OnPropertyChanged();
                    OnSelectedLayerChanged();
                }
            }
        }

        public ICommand LoadFileCommand { get; }

        public MainViewModel()
        {
            LoadFileCommand = new RelayCommand(ExecuteLoadFileCommand);
        }

        private void ExecuteLoadFileCommand()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "OVF Files (*.ovf)|*.ovf|All files (*.*)|*.*",
                Title = "Select an OVF File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                using (var reader = new OVFFileReader())
                {
                    try
                    {
                        reader.OpenJob(openFileDialog.FileName);
                        _loadedJob = reader.CacheJobToMemory();
                        JobName = _loadedJob.JobMetaData?.JobName ?? "Unnamed Job";
                        UpdateLayerSummaries();
                    }
                    catch (Exception ex)
                    {
                        JobName = "Failed to load file!";
                        LayerSummaries.Clear();
                        MessageBox.Show($"Error loading OVF file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void UpdateLayerSummaries()
        {
            LayerSummaries.Clear();
            if (_loadedJob == null) return;

            for (int i = 0; i < _loadedJob.WorkPlanes.Count; i++)
            {
                var plane = _loadedJob.WorkPlanes[i];
                var blockCount = plane.VectorBlocks?.Count ?? 0;
                var zHeight = plane.ZPosInMm.ToString("F2");
                LayerSummaries.Add($"Layer {i + 1}: Z={zHeight}mm, {blockCount} blocks");
            }

            // Select the first layer by default if available
            SelectedLayerSummary = LayerSummaries.FirstOrDefault();
        }

        private void OnSelectedLayerChanged()
        {
            if (_loadedJob == null || SelectedLayerSummary == null) return;

            // Get the layer index from the selected summary string
            if (int.TryParse(SelectedLayerSummary.Split(' ')[1].TrimEnd(':'), out int layerIndex))
            {
                // Convert to 0-based index
                layerIndex--;
                if (layerIndex >= 0 && layerIndex < _loadedJob.WorkPlanes.Count)
                {
                    var selectedPlane = _loadedJob.WorkPlanes[layerIndex];
                    // TODO: Update canvas with the selected layer's vectors
                }
            }
        }
    }
}