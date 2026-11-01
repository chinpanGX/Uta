using NetArchTest.Rules;
using Uta.Server.Infrastructure;

namespace Uta.Server.Tests.Architecture;

public class ArchitectureTests
{
    private static PredicateList LayerTypes(string layer) =>
        Types.InAssembly(typeof(PlayerResolver).Assembly).That().ResideInNamespaceStartingWith($"Uta.Server.{layer}");

    [Fact]
    public void Api_DoesNotDependOnInfrastructure()
    {
        var result = LayerTypes("Api").ShouldNot().HaveDependencyOnAny("Uta.Server.Infrastructure").GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_DoesNotDependOnOuterLayers()
    {
        var result = LayerTypes("Application").ShouldNot()
            .HaveDependencyOnAny("Uta.Server.Api", "Uta.Server.Infrastructure", "Uta.Shared.Contracts").GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_DoesNotDependOnOuterLayersOrDataAccess()
    {
        var result = LayerTypes("Domain").ShouldNot()
            .HaveDependencyOnAny("Uta.Server.Api", "Uta.Server.Application", "Uta.Server.Infrastructure", "Uta.Shared.Contracts", "Npgsql", "Dapper")
            .GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = LayerTypes("Infrastructure").ShouldNot().HaveDependencyOnAny("Uta.Server.Api").GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void InfrastructureNamespace_HasTypes()
    {
        Assert.NotEmpty(LayerTypes("Infrastructure").GetTypes());
    }

    [Fact]
    public void ApiNamespace_HasTypes()
    {
        Assert.NotEmpty(LayerTypes("Api").GetTypes());
    }

    [Fact]
    public void ApplicationNamespace_HasTypes()
    {
        Assert.NotEmpty(LayerTypes("Application").GetTypes());
    }

    [Fact]
    public void Rule_DetectsViolation()
    {
        var result = LayerTypes("Infrastructure").ShouldNot().HaveDependencyOn("Npgsql").GetResult();
        Assert.False(result.IsSuccessful);
    }
}
