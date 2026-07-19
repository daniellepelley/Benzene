using System.Linq;
using Benzene.Core.Messages;
using Benzene.Extras.ResponseEvents;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Extras.ResponseEvents;

public class ResponseEventMappingTest
{
    private class OrderPayload
    {
        public string Id { get; set; }
    }

    [Fact]
    public void ExplicitMapping_SuccessfulResultWithPayload_Publishes()
    {
        var mapping = new ExplicitResponseEventMapping("order:create", "order:created");

        var publication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Created(new OrderPayload { Id = "42" }));

        Assert.NotNull(publication);
        Assert.Equal("order:created", publication.EventTopic);
        Assert.Equal("42", ((OrderPayload)publication.Payload).Id);
    }

    [Fact]
    public void ExplicitMapping_TopicMatchIsCaseInsensitive()
    {
        var mapping = new ExplicitResponseEventMapping("Order:Create", "order:created");

        var publication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Ok(new OrderPayload()));

        Assert.NotNull(publication);
    }

    [Fact]
    public void ExplicitMapping_DifferentTopic_DoesNotPublish()
    {
        var mapping = new ExplicitResponseEventMapping("order:create", "order:created");

        var publication = mapping.Resolve(new Topic("invoice:create"), BenzeneResult.Created(new OrderPayload()));

        Assert.Null(publication);
    }

    [Fact]
    public void ExplicitMapping_FailedResult_DoesNotPublish()
    {
        var mapping = new ExplicitResponseEventMapping("order:create", "order:created");

        var publication = mapping.Resolve(new Topic("order:create"), BenzeneResult.NotFound<OrderPayload>());

        Assert.Null(publication);
    }

    [Fact]
    public void ExplicitMapping_SuccessfulResultWithoutPayload_DoesNotPublish()
    {
        var mapping = new ExplicitResponseEventMapping("order:create", "order:created");

        var publication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Accepted<OrderPayload>());

        Assert.Null(publication);
    }

    [Fact]
    public void ExplicitMapping_WhenPredicate_ReplacesDefaultStatusCheck()
    {
        var mapping = new ExplicitResponseEventMapping("order:create", "order:created",
            when: result => result.Status == BenzeneResultStatus.Created);

        var okPublication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Ok(new OrderPayload()));
        var createdPublication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Created(new OrderPayload()));

        Assert.Null(okPublication);
        Assert.NotNull(createdPublication);
    }

    [Fact]
    public void ExplicitMapping_Projector_ReshapesPayload()
    {
        var mapping = new ExplicitResponseEventMapping("order:create", "order:created",
            projectPayload: payload => ((OrderPayload)payload).Id);

        var publication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Created(new OrderPayload { Id = "42" }));

        Assert.NotNull(publication);
        Assert.Equal("42", publication.Payload);
    }

    [Fact]
    public void ExplicitMapping_ProjectorReturningNull_DoesNotPublish()
    {
        var mapping = new ExplicitResponseEventMapping("order:create", "order:created",
            projectPayload: _ => null);

        var publication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Created(new OrderPayload()));

        Assert.Null(publication);
    }

    [Theory]
    [InlineData("order:create", BenzeneResultStatus.Created, "order:created")]
    [InlineData("order:update", BenzeneResultStatus.Updated, "order:updated")]
    [InlineData("order:delete", BenzeneResultStatus.Deleted, "order:deleted")]
    public void CrudConvention_MatchingVerbAndStatus_PublishesPastTenseTopic(string sourceTopic, string status, string expectedEventTopic)
    {
        var mapping = new CrudConventionResponseEventMapping();

        var publication = mapping.Resolve(new Topic(sourceTopic), BenzeneResult.Set(status, new OrderPayload(), true));

        Assert.NotNull(publication);
        Assert.Equal(expectedEventTopic, publication.EventTopic);
    }

    [Fact]
    public void CrudConvention_MatchingVerbWithWrongStatus_DoesNotPublish()
    {
        var mapping = new CrudConventionResponseEventMapping();

        var publication = mapping.Resolve(new Topic("order:create"), BenzeneResult.Ok(new OrderPayload()));

        Assert.Null(publication);
    }

    [Fact]
    public void CrudConvention_NonCrudVerb_DoesNotPublish()
    {
        var mapping = new CrudConventionResponseEventMapping();

        var publication = mapping.Resolve(new Topic("order:submit"), BenzeneResult.Created(new OrderPayload()));

        Assert.Null(publication);
    }

    [Fact]
    public void Mappings_EveryMatchingMappingPublishes()
    {
        var mappings = new ResponseEventMappings(new IResponseEventMapping[]
        {
            new ExplicitResponseEventMapping("order:create", "order:created"),
            new ExplicitResponseEventMapping("order:create", "audit:order-created"),
            new ExplicitResponseEventMapping("invoice:create", "invoice:created"),
        }, PublishFailureMode.FailMessage);

        var publications = mappings.Resolve(new Topic("order:create"), BenzeneResult.Created(new OrderPayload()));

        Assert.Equal(new[] { "order:created", "audit:order-created" }, publications.Select(x => x.EventTopic));
    }

    [Fact]
    public void Mappings_NoMatch_ResolvesEmpty()
    {
        var mappings = new ResponseEventMappings(new IResponseEventMapping[]
        {
            new ExplicitResponseEventMapping("order:create", "order:created"),
        }, PublishFailureMode.FailMessage);

        var publications = mappings.Resolve(new Topic("customer:create"), BenzeneResult.Created(new OrderPayload()));

        Assert.Empty(publications);
    }
}
