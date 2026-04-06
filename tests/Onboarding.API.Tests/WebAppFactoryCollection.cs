namespace Onboarding.API.Tests;

/// <summary>
/// xUnit collection that serializes all WebApplicationFactory-based test classes.
/// Multiple WAF instances started in parallel can fail with "entry point exited without
/// building an IHost" because Program.cs uses a top-level try/catch. Grouping them in
/// a single [Collection] forces sequential execution within the collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WebAppFactoryCollection : ICollectionFixture<object>
{
    public const string Name = "WebApplicationFactory";
}
