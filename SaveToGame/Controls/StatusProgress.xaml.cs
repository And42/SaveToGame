using System.Threading;
using Interfaces.OrganisationItems;
using MVVM_Tools.Code.Providers;

namespace SaveToGameWpf.Controls
{
    public partial class StatusProgress
    {
        private class VisualProgress : IVisualProgress
        {
            private readonly StatusProgress _control;

            public VisualProgress(StatusProgress control)
            {
                _control = control;
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
        }

        private readonly IVisualProgress _visualProgress;

        public IProperty<string> StatusLabel { get; } = new FieldProperty<string>();

        public IProperty<int> StatusProgressNow { get; } = new FieldProperty<int>();
        public IProperty<bool> StatusProgressIndeterminate { get; } = new FieldProperty<bool>();
        public IProperty<bool> StatusProgressVisible { get; } = new FieldProperty<bool>();

        public IProperty<bool> StatusProgressLabelVisible { get; } = new FieldProperty<bool>();

        public IProperty<bool> StatusIndeterminateLabelVisible { get; } = new FieldProperty<bool>();
        public IProperty<string> StatusIndeterminateLabelText { get; } = new FieldProperty<string>();

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
