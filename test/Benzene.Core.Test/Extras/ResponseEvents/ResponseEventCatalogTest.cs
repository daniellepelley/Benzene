using System;
using System.Linq;
using Benzene.Abstractions.Messages;
using Benzene.Extras.ResponseEvents;
using Xunit;

namespace Benzene.Test.Extras.ResponseEvents;

public class ResponseEventCatalogTest
{
    private class OrderCreatedEvent
    {
    }

    private class InvoiceSubmittedEvent
    {
    }

    [Fact]
    public void Catalog_AggregatesMappingsAcrossPipelines()
    {
        var sqsPipeline = new ResponseEventMappings(new IResponseEventMapping[]
        {
            new ExplicitResponseEventMapping("order:create", "order:created", typeof(OrderCreatedEvent)),
        }, PublishFailureMode.FailMessage);
        var serviceBusPipeline = new ResponseEventMappings(new IResponseEventMapping[]
        {
            new ExplicitResponseEventMapping("invoice:submit", "invoice:submitted"),
            new CrudConventionResponseEventMapping(),
        }, PublishFailureMode.LogAndContinue);

        var catalog = new ResponseEventCatalog(new[] { sqsPipeline, serviceBusPipeline }, Array.Empty<ResponseEventDeclarations>());

        Assert.Equal(3, catalog.Mappings.Count);
        Assert.Contains(catalog.Mappings, x => x.SourceTopic == "order:create" && x.EventTopic == "order:created");
        Assert.Contains(catalog.Mappings, x => x.SourceTopic == "invoice:submit");
        Assert.Contains(catalog.Mappings, x => x is CrudConventionResponseEventMapping);
    }

    [Fact]
    public void FindDefinitions_ReturnsOnlyMappingsWithDeclaredPayloadType()
    {
        var catalog = new ResponseEventCatalog(new[]
        {
            new ResponseEventMappings(new IResponseEventMapping[]
            {
                new ExplicitResponseEventMapping("order:create", "order:created", typeof(OrderCreatedEvent)),
                new ExplicitResponseEventMapping("invoice:submit", "invoice:submitted"),
                new CrudConventionResponseEventMapping(),
            }, PublishFailureMode.FailMessage),
        }, Array.Empty<ResponseEventDeclarations>());

        var definitions = catalog.FindDefinitions();

        var definition = Assert.Single(definitions);
        Assert.Equal("order:created", definition.Topic.Id);
        Assert.Equal(typeof(OrderCreatedEvent), definition.RequestType);
    }

    [Fact]
    public void FindDefinitions_IncludesDeclarationOnlyDefinitions()
    {
        var declarations = new ResponseEventDeclarations(new IMessageDefinition[]
        {
            new ResponseEventDefinition("invoice:submitted", typeof(InvoiceSubmittedEvent)),
        });
        var catalog = new ResponseEventCatalog(new[]
        {
            new ResponseEventMappings(new IResponseEventMapping[]
            {
                new ExplicitResponseEventMapping("order:create", "order:created", typeof(OrderCreatedEvent)),
            }, PublishFailureMode.FailMessage),
        }, new[] { declarations });

        var definitions = catalog.FindDefinitions();

        Assert.Equal(2, definitions.Length);
        Assert.Contains(definitions, x => x.Topic.Id == "order:created" && x.RequestType == typeof(OrderCreatedEvent));
        Assert.Contains(definitions, x => x.Topic.Id == "invoice:submitted" && x.RequestType == typeof(InvoiceSubmittedEvent));
        Assert.Equal(declarations.Definitions, catalog.DeclaredDefinitions.ToArray());
    }

    [Fact]
    public void Mappings_DescribeThemselves()
    {
        var typed = new ExplicitResponseEventMapping("order:create", "order:created", typeof(OrderCreatedEvent));
        var conditional = new ExplicitResponseEventMapping("invoice:submit", "invoice:submitted", when: _ => true);

        Assert.Equal("order:create -> order:created (OrderCreatedEvent)", typed.Description);
        Assert.Equal("invoice:submit -> invoice:submitted [conditional]", conditional.Description);
        Assert.NotEmpty(new CrudConventionResponseEventMapping().Description);
    }
}
