namespace SaveToGameWpf.Logic.Interfaces
{
    public interface IVisualProgress
    {
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