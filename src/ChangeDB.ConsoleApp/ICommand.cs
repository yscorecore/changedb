namespace ChangeDB.ConsoleApp
{
    interface ICommand
    {
        string CommandName { get; }
        int Run();
    }
}
