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
    public bool Write { get; init; }

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
    internal class ModeMeasures
    {
        public long TxCount;
        public long TotalTime;
        public long BeginTime;
        public long Conversion;
        public long Query;
        public long Commit;
        public long Retried;
        public long MaxTotalTime;
        public long MaxBeginTime;
        public long MaxConversion;
        public long MaxQuery;
        public long MaxCommit;
    }

    private static ModeMeasures Writes = new();
    private static ModeMeasures Reads = new();

    internal static void Register(TransactionMeasures transactionMeasures)
    {
        var measures = transactionMeasures.Write ? Writes : Reads;
        measures.TxCount++;
        measures.TotalTime += transactionMeasures.TotalTime.ElapsedMilliseconds;
        measures.BeginTime += transactionMeasures.Begin.ElapsedMilliseconds;
        measures.Query += transactionMeasures.Query.ElapsedMilliseconds;
        measures.Conversion += transactionMeasures.Conversion.ElapsedMilliseconds;
        measures.Commit += transactionMeasures.Commit.ElapsedMilliseconds;
        measures.Retried += transactionMeasures.RetryCount > 0 ? 1 : 0;

        if (measures.MaxTotalTime < transactionMeasures.TotalTime.ElapsedMilliseconds) measures.MaxTotalTime = transactionMeasures.TotalTime.ElapsedMilliseconds;
        if (measures.MaxBeginTime < transactionMeasures.Begin.ElapsedMilliseconds) measures.MaxBeginTime = transactionMeasures.Begin.ElapsedMilliseconds;
        if (measures.MaxConversion < transactionMeasures.Conversion.ElapsedMilliseconds) measures.MaxConversion = transactionMeasures.Conversion.ElapsedMilliseconds;
        if (measures.MaxQuery < transactionMeasures.Query.ElapsedMilliseconds) measures.MaxQuery = transactionMeasures.Query.ElapsedMilliseconds;
        if (measures.MaxCommit < transactionMeasures.Commit.ElapsedMilliseconds) measures.MaxCommit = transactionMeasures.Commit.ElapsedMilliseconds;
    }

    public static void Log()
    {
        if (Writes.TxCount != 0)
        {
            PrintMeasures(Writes);
        }
        if (Reads.TxCount != 0)
        {
            PrintMeasures(Reads);
        }
    }

    private static void PrintMeasures(ModeMeasures measures)
    {
        Console.WriteLine($"{Environment.NewLine}" +
                          $"--Reads Summary--{Environment.NewLine}" +
                          $"Count      :{measures.TxCount:D5}{Environment.NewLine}" +
                          $"Retries    :{measures.Retried:D5}{Environment.NewLine}" +
                          $"--Averages-{Environment.NewLine}" +
                          $"Tx Time    :{measures.TotalTime / measures.TxCount:D5}{Environment.NewLine}" +
                          $"Begin      :{measures.BeginTime / measures.TxCount:D5}{Environment.NewLine}" +
                          $"Query      :{measures.Query / measures.TxCount:D5}{Environment.NewLine}" +
                          $"Conversion :{measures.Conversion / measures.TxCount:D5}{Environment.NewLine}" +
                          $"Commit     :{measures.Commit / measures.TxCount:D5}{Environment.NewLine}" +
                          $"----Max----{Environment.NewLine}" +
                          $"Tx Time    :{measures.MaxTotalTime:D5}{Environment.NewLine}" +
                          $"Begin      :{measures.MaxBeginTime:D5}{Environment.NewLine}" +
                          $"Query      :{measures.MaxQuery:D5}{Environment.NewLine}" +
                          $"Conversion :{measures.MaxConversion:D5}{Environment.NewLine}" +
                          $"Commit     :{measures.MaxCommit:D5}{Environment.NewLine}" +
                          $"-----------");
    }
}