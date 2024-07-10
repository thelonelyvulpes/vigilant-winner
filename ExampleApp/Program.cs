using Neo4j.Driver.Mapping;

await using var client = new Neo4jClient();

var rtl = await client.EstimateRoundtripLatency();
Console.WriteLine($"round trip time {rtl}ms");

for (var i = 0; i < 100; i++) await client.MeasuringTransactions("RETURN 1", null);

for (var i = 0; i < 100; i++)
{
    var foos = await client.MeasuringTransactions<Example>("RETURN 1 as foo", null);
}

SystemMeasures.Log();


internal class Example
{
    [MappingSource("foo")] public int Foo { get; set; }
}