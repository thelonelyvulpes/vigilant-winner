await using var client = new Neo4jClient();

var rtl = await client.EstimateRoundtripLatency();
Console.WriteLine($"round trip time {rtl}ms");

for (var i = 0; i < 100; i++) await client.MeasuringTransactions("RETURN 1", null);

SystemMeasures.Log();