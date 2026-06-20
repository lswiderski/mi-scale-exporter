using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiScaleExporter.Models
{
    public class ScaleMeasurement : INotifyPropertyChanged 
    {

        private ScaleMeasurement() { }
        public static ScaleMeasurement Instance { get; } = new ScaleMeasurement();

        private string _weight;
        public string Weight
        {
            get => _weight;
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(Weight)));
                }
            }
        }

        private string _foundScale;
        public string FoundScale
        {
            get => _foundScale;
            set
            {
                if (_foundScale != value)
                {
                    _foundScale = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(FoundScale)));
                }
            }
        }

        private string _debugData;
        public string DebugData
        {
            get => _debugData;
            set
            {
                if (_debugData != value)
                {
                    _debugData = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(DebugData)));
                }
            }
        }

        private string _rawData;
        public string RawData
        {
            get => _rawData;
            set
            {
                if (_rawData != value)
                {
                    _rawData = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(RawData)));
                }
            }
        }

        private ScanPhase _phase = ScanPhase.Idle;
        public ScanPhase Phase
        {
            get => _phase;
            set
            {
                if (_phase != value)
                {
                    _phase = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Phase)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScanning)));
                }
            }
        }

        private string _phaseLabel;
        public string PhaseLabel
        {
            get => _phaseLabel;
            set
            {
                if (_phaseLabel != value)
                {
                    _phaseLabel = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PhaseLabel)));
                }
            }
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
                }
            }
        }

        public bool IsScanning
        {
            get
            {
                return _phase == ScanPhase.Searching
                    || _phase == ScanPhase.ScaleFound
                    || _phase == ScanPhase.Reading
                    || _phase == ScanPhase.Stabilizing;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
