using System.Diagnostics;

internal class TransactionMeasures : IDisposable
{
    public Stopwatch Begin = Stopwatch.StartNew();
    public Stopwatch Commit = new();
    public Stopwatch Conversion = new();
    public Stopwatch Query = new();
    public int RetryCount = 0;
    public Stopwatch TotalTime = Stopwatch.StartNew();
    public bool LogMeasures { get; init; }

    public void Dispose()
    {
        TotalTime.Stop();
        SystemMeasures.Register(this);
        if (LogMeasures) Log();
    }

    public void Log()
    {
        Console.WriteLine($"Timing:{Environment.NewLine}" +
                          $"Total     :{TotalTime.ElapsedMilliseconds:D5}{Environment.NewLine}" +
                          $"Begin     :{Begin.ElapsedMilliseconds:D5}{Environment.NewLine}" +
                          $"Query     :{Query.ElapsedMilliseconds:D5}{Environment.NewLine}" +
                          $"Conversion:{Conversion.ElapsedMilliseconds:D5}{Environment.NewLine}" +
                          $"Commit    :{Commit.ElapsedMilliseconds:D5}{Environment.NewLine}" +
                          $"Retries   :{RetryCount:D5}{Environment.NewLine}" +
                          $"--");
    }
}

internal class ExecuteMeasures : IDisposable
{
    public Stopwatch TotalTime = Stopwatch.StartNew();
    public bool LogMeasures { get; init; }

    public void Dispose()
    {
        TotalTime.Stop();
        if (LogMeasures) Log();
    }

    public void Log()
    {
        Console.WriteLine($"Timing:{Environment.NewLine}" +
                          $"Total     :{TotalTime.ElapsedMilliseconds:D5}{Environment.NewLine}" +
                          $"--");
    }
}

internal static class SystemMeasures
{
    public static long TxCount;
    public static long TotalTime;
    public static long BeginTime;
    public static long Conversion;
    public static long Query;
    public static long Commit;
    public static long Retried;

    public static long MaxTotalTime;
    public static long MaxBeginTime;
    public static long MaxConversion;
    public static long MaxQuery;
    public static long MaxCommit;

    internal static void Register(TransactionMeasures transactionMeasures)
    {
        TxCount++;
        TotalTime += transactionMeasures.TotalTime.ElapsedMilliseconds;
        BeginTime += transactionMeasures.Begin.ElapsedMilliseconds;
        Query += transactionMeasures.Query.ElapsedMilliseconds;
        Conversion += transactionMeasures.Conversion.ElapsedMilliseconds;
        Commit += transactionMeasures.Commit.ElapsedMilliseconds;
        Retried += transactionMeasures.RetryCount > 0 ? 1 : 0;

        if (MaxTotalTime < transactionMeasures.TotalTime.ElapsedMilliseconds)
            MaxTotalTime = transactionMeasures.TotalTime.ElapsedMilliseconds;
        if (MaxBeginTime < transactionMeasures.Begin.ElapsedMilliseconds)
            MaxBeginTime = transactionMeasures.Begin.ElapsedMilliseconds;
        if (MaxConversion < transactionMeasures.Conversion.ElapsedMilliseconds)
            MaxConversion = transactionMeasures.Conversion.ElapsedMilliseconds;
        if (MaxQuery < transactionMeasures.Query.ElapsedMilliseconds)
            MaxQuery = transactionMeasures.Query.ElapsedMilliseconds;
        if (MaxCommit < transactionMeasures.Commit.ElapsedMilliseconds)
            MaxCommit = transactionMeasures.Commit.ElapsedMilliseconds;
    }

    public static void Log()
    {
        Console.WriteLine($"{Environment.NewLine}" +
                          $"--Summary--{Environment.NewLine}" +
                          $"Count      :{TxCount:D5}{Environment.NewLine}" +
                          $"Retries    :{Retried:D5}{Environment.NewLine}" +
                          $"--Averages-{Environment.NewLine}" +
                          $"Tx Time    :{TotalTime / TxCount:D5}{Environment.NewLine}" +
                          $"Begin      :{BeginTime / TxCount:D5}{Environment.NewLine}" +
                          $"Query      :{Query / TxCount:D5}{Environment.NewLine}" +
                          $"Conversion :{Conversion / TxCount:D5}{Environment.NewLine}" +
                          $"Commit     :{Commit / TxCount:D5}{Environment.NewLine}" +
                          $"----Max----{Environment.NewLine}" +
                          $"Tx Time    :{MaxTotalTime:D5}{Environment.NewLine}" +
                          $"Begin      :{MaxBeginTime:D5}{Environment.NewLine}" +
                          $"Query      :{MaxQuery:D5}{Environment.NewLine}" +
                          $"Conversion :{MaxConversion:D5}{Environment.NewLine}" +
                          $"Commit     :{MaxCommit:D5}{Environment.NewLine}" +
                          $"-----------");
    }
}