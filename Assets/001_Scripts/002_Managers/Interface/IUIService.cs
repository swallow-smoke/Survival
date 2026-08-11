namespace _001_Scripts.Interface
{
    public interface IUIPanelNavigator
    {
        void OpenPanel(string panelKey);
        void OpenWorkbench();
        void OpenSubmarineFabricator();
    }

    public interface IUIService : IManager, IUIPanelNavigator { }
}
