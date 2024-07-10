using Neo4j.Driver;

public class SimpleConsoleLogger : ILogger
{
    public void Error(Exception cause, string message, params object[] args)
    {
        Console.WriteLine(cause);
    }

    public void Warn(Exception cause, string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public void Info(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public void Debug(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public void Trace(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public bool IsTraceEnabled()
    {
        return false;
    }

    public bool IsDebugEnabled()
    {
        return false;
    }
}