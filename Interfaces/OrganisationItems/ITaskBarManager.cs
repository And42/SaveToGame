namespace Interfaces.OrganisationItems
{
    public interface ITaskBarManager
    {
        void SetProgress(int current, int maximum = 100);

        void SetIndeterminateState();

        void SetUsualState();

        void SetNoneState();
    }
}