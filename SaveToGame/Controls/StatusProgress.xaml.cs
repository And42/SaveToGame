using System;
using System.ComponentModel;
using System.Threading;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.Interfaces;

namespace SaveToGameWpf.Controls
{
    public partial class StatusProgress
    {
        private class VisualProgress : IVisualProgress
        {
            private readonly StatusProgress _control;

            public event Action<(int current, int maximum)> ProgressChanged;

            public VisualProgress(StatusProgress control)
            {
                _control = control;

                _control.StatusProgressNow.PropertyChanged += OnProgressChanged;
            }

            public void SetLabelText(string text)
            {
                _control.StatusLabel.Value = text;
            }

            public void ShowIndeterminateLabel()
            {
                _control.StatusIndeterminateLabelVisible.Value = true;
            }

            public void HideIndeterminateLabel()
            {
                _control.StatusIndeterminateLabelVisible.Value = false;
            }

            public void ShowBar()
            {
                _control.StatusProgressVisible.Value = true;
                _control.StatusProgressLabelVisible.Value = !_control.StatusProgressIndeterminate.Value;
            }

            public void HideBar()
            {
                _control.StatusProgressVisible.Value = false;
                _control.StatusProgressLabelVisible.Value = false;
            }

            public void SetBarIndeterminate()
            {
                _control.StatusProgressIndeterminate.Value = true;
                _control.StatusProgressLabelVisible.Value = false;
            }

            public void SetBarUsual()
            {
                _control.StatusProgressIndeterminate.Value = false;
                _control.StatusProgressLabelVisible.Value = _control.StatusProgressVisible.Value;
            }

            public void SetBarValue(int value)
            {
                _control.StatusProgressNow.Value = value;
            }

            private void OnProgressChanged(object sender, PropertyChangedEventArgs e)
            {
                ProgressChanged?.Invoke((_control.StatusProgressNow.Value, 100));
            }
        }

        private readonly IVisualProgress _visualProgress;

        public Property<string> StatusLabel { get; } = new Property<string>();

        public Property<int> StatusProgressNow { get; } = new Property<int>();
        public Property<bool> StatusProgressIndeterminate { get; } = new Property<bool>();
        public Property<bool> StatusProgressVisible { get; } = new Property<bool>();

        public Property<bool> StatusProgressLabelVisible { get; } = new Property<bool>();

        public Property<bool> StatusIndeterminateLabelVisible { get; } = new Property<bool>();
        public Property<string> StatusIndeterminateLabelText { get; } = new Property<string>();

        private Timer _indeterminateTimer;

        public StatusProgress()
        {
            InitializeComponent();

            _visualProgress = new VisualProgress(this);

            StatusIndeterminateLabelVisible.PropertyChanged += (sender, args) =>
            {
                if (StatusIndeterminateLabelVisible.Value)
                    StartTimer();
                else
                    StopTimer();
            };
        }

        public IVisualProgress GetVisualProgress() => _visualProgress;

        private void StartTimer()
        {
            if (_indeterminateTimer != null)
                return;

            _indeterminateTimer = new Timer(state =>
            {
                string current = StatusIndeterminateLabelText.Value;

                if (string.IsNullOrEmpty(current))
                    StatusIndeterminateLabelText.Value = ".";
                else if (current.Length == 3)
                    StatusIndeterminateLabelText.Value = string.Empty;
                else
                    StatusIndeterminateLabelText.Value += ".";
            }, null, 0, 500);
        }

        private void StopTimer()
        {
            if (_indeterminateTimer == null)
                return;

            _indeterminateTimer.Dispose();
            _indeterminateTimer = null;
        }
    }
}
