using System.Diagnostics;
using Neo4j.Driver;
using Neo4j.Driver.Mapping;

// ReSharper disable AccessToDisposedClosure

public sealed class Neo4jClient : IAsyncDisposable
{
    private readonly string database;
    private readonly IDriver driver;

    public Neo4jClient()
    {
        driver = GraphDatabase.Driver("bolt://127.0.0.1", AuthTokens.Basic("neo4j", "password"),
            x => x.WithLogger(new SimpleConsoleLogger()));

        // Always specify the database!
        // beginning a transaction without specifying a database will take twice as long.
        database = "neo4j";
    }

    /// <summary>
    ///     Used to reduce number of allocations when reading records
    /// </summary>
    private int InitialListCapacity => 10;

    private TimeSpan? WriteTimeout => TimeSpan.FromSeconds(10);
    private TimeSpan? ReadTimeout => TimeSpan.FromSeconds(20);

    public ValueTask DisposeAsync()
    {
        return driver.DisposeAsync();
    }

    public Task EnsureConnectedAsync()
    {
        return driver.VerifyConnectivityAsync();
    }

    /// <summary>
    ///     Use this to understand the cost of network round trips.
    /// </summary>
    /// <returns></returns>
    public async Task<long> EstimateRoundtripLatency()
    {
        await EnsureConnectedAsync();
        await using var session = driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            for (var i = 0; i < 50; i++)
            {
                var warmup = await tx.RunAsync("RETURN 1");
                await warmup.ConsumeAsync();
            }

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 100; i++)
            {
                var c = await tx.RunAsync("RETURN 1");
                await c.ConsumeAsync();
            }

            return sw.ElapsedMilliseconds / 100;
        });
    }

    /// <summary>
    ///     Execute query uses 1 less round trip that session.ExecuteRead/Write
    ///     Only allows 1 query per transaction
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <param name="write"></param>
    /// <param name="logMeasures"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<List<T>> FasterStartMeasuringSingleQueryTransactions<T>(string query, object parameters,
        bool write = false,
        bool logMeasures = true)
    {
        // Known issue, can not set time out for transactions (will be fixed for 5.23!)
        using var measures = new ExecuteMeasures { LogMeasures = logMeasures };
        var result = await driver.ExecutableQuery(query)
            .WithParameters(parameters)
            .WithConfig(new QueryConfig(write ? RoutingControl.Writers : RoutingControl.Readers, database))
            .ExecuteAsync()
            // https://neo4j.com/developer-blog/object-mapping-neo4j-driver-net/
            // Read about the in built mapping
            .AsObjectsAsync<T>();
        return result.ToList();
    }


    public async Task<List<T>> MeasuringTransactions<T>(string query, object? parameters, bool write = false,
        bool logMeasures = false)
    {
        await using var session = driver.AsyncSession(x => x.WithDatabase(database));
        using var measures = new TransactionMeasures { LogMeasures = logMeasures, Write = write, MapLess = false };
        var result = await (write
            ? session.ExecuteWriteAsync(tx => RunInTx<T>(tx, measures, query, parameters),
                cfg => cfg.WithTimeout(WriteTimeout))
            : session.ExecuteReadAsync(tx => RunInTx<T>(tx, measures, query, parameters),
                cfg => cfg.WithTimeout(ReadTimeout)));
        measures.Commit.Stop();
        return result;
    }

    public async Task MeasuringTransactions(string query, object? parameters, bool write = false,
        bool logMeasures = false)
    {
        await using var session = driver.AsyncSession(x => x.WithDatabase(database));
        using var measures = new TransactionMeasures { LogMeasures = logMeasures, Write = write, MapLess = true };

        await (write
            ? session.ExecuteWriteAsync(tx => RunInTx(tx, measures, query, parameters))
            : session.ExecuteReadAsync(tx => RunInTx(tx, measures, query, parameters)));
        measures.Commit.Stop();
    }

    private async Task<List<T>> RunInTx<T>(IAsyncQueryRunner tx, TransactionMeasures transactionMeasures, string query,
        object? parameters)
    {
        try
        {
            transactionMeasures.Begin.Stop();
            transactionMeasures.Query.Restart();
            var cursor = await tx.RunAsync(query, parameters);

            var records = await cursor.ToListAsync(InitialListCapacity);
            transactionMeasures.Query.Stop();
            transactionMeasures.Conversion.Restart();
            var result = records.Select(x =>
            {
                // https://neo4j.com/developer-blog/object-mapping-neo4j-driver-net/
                // Read about the in built mapping
                return x.AsObject<T>();
            }).ToList();
            transactionMeasures.Conversion.Stop();
            transactionMeasures.Commit.Restart();
            return result;
        }
        catch (Exception)
        {
            transactionMeasures.RetryCount++;
            transactionMeasures.Begin.Restart();
            throw;
        }
    }

    private async Task RunInTx(IAsyncQueryRunner tx, TransactionMeasures transactionMeasures, string query,
        object? parameters)
    {
        try
        {
            transactionMeasures.Begin.Stop();
            transactionMeasures.Query.Restart();
            var cursor = await tx.RunAsync(query, parameters);
            await cursor.ConsumeAsync();
            transactionMeasures.Query.Stop();
            transactionMeasures.Commit.Restart();
        }
        catch (Exception)
        {
            transactionMeasures.RetryCount++;
            transactionMeasures.Begin.Restart();
            throw;
        }
    }
}