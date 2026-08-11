namespace _001_Scripts.Interface
{
    public interface IOpenable
    {
        void Open();
    }

    public interface ICloseable
    {
        void Close();
    }

    public interface IPanelService : IOpenable, ICloseable { }
}
