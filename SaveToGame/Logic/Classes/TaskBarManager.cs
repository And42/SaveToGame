using System;
using System.Windows.Shell;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Logic.Classes
{
    internal class TaskBarManager : ITaskBarManager
    {
        private const int MaxProgress = 100;
        private const double MaxProgressDouble = MaxProgress;

        private readonly TaskbarItemInfo _taskbarItemInfo;

        private TaskbarItemProgressState _currentState = TaskbarItemProgressState.None;
        private int _currentProgress;

        public TaskBarManager(TaskbarItemInfo taskbarItemInfo)
        {
            _taskbarItemInfo = taskbarItemInfo ?? throw new ArgumentNullException(nameof(taskbarItemInfo));

            _taskbarItemInfo.ProgressState = _currentState;
            _taskbarItemInfo.ProgressValue = _currentProgress;
        }

        public void SetProgress(int current, int maximum = MaxProgress)
        {
            if (maximum < 0)
                throw new ArgumentOutOfRangeException(nameof(maximum));

            if (maximum != MaxProgress)
                current = current * 100 / maximum;

            if (current == _currentProgress)
                return;

            _currentProgress = current;

            UpdateProgress();
        }

        public void SetIndeterminateState()
        {
            SetState(TaskbarItemProgressState.Indeterminate);
        }

        public void SetUsualState()
        {
            SetState(TaskbarItemProgressState.Normal);
        }

        public void SetNoneState()
        {
            SetState(TaskbarItemProgressState.None);
        }

        private void UpdateProgress()
        {
            _taskbarItemInfo.Dispatcher.InvokeAction(() => _taskbarItemInfo.ProgressValue = _currentProgress / MaxProgressDouble);
        }

        private void SetState(TaskbarItemProgressState state)
        {
            if (_currentState == state)
                return;

            _currentState = state;

            UpdateState();
        }

        private void UpdateState()
        {
            _taskbarItemInfo.Dispatcher.InvokeAction(() => _taskbarItemInfo.ProgressState = _currentState);
        }
    }
}
