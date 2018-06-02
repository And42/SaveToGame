using System;

namespace SaveToGameWpf.Logic.Interfaces
{
    public interface IVisualProgress
    {
        event Action<(int current, int maximum)> ProgressChanged; 

        void SetLabelText(string text);

        void ShowIndeterminateLabel();

        void HideIndeterminateLabel();

        void ShowBar();

        void HideBar();

        void SetBarIndeterminate();

        void SetBarUsual();

        void SetBarValue(int value);
    }
}