using Benzene.Extras.ResponseEvents;
using Xunit;

namespace Benzene.Test.Extras.ResponseEvents;

public class ResponseEventCatalogTest
{
    private class OrderCreatedEvent
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

        var catalog = new ResponseEventCatalog(new[] { sqsPipeline, serviceBusPipeline });

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
        });

        var definitions = catalog.FindDefinitions();

        var definition = Assert.Single(definitions);
        Assert.Equal("order:created", definition.Topic.Id);
        Assert.Equal(typeof(OrderCreatedEvent), definition.RequestType);
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
