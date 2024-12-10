using Benzene.Aws.Core;
using Benzene.Examples.App.Data;
using Benzene.Examples.Aws.Tests.Helpers;

namespace Benzene.Examples.Aws.Tests.Integration;

public abstract class InMemoryOrdersTestBase
{
    protected TestLambdaHosting TestLambdaHosting;

    protected InMemoryOrdersTestBase(IAwsLambdaEntryPoint lambdaEntryPoint)
    {
        SetUp();
        TestLambdaHosting = new TestLambdaHosting(lambdaEntryPoint);
    }

    public Order[] GetPersistedOrders()
    {
        return InMemoryOrderDbClient.Orders.ToArray();
    }

    public void AddOrder(Order order)
    {
        InMemoryOrderDbClient.Orders.Add(order);
    }

    private void SetUp()
    {
        EnvironmentSetUp.SetUp();
        InMemoryOrderDbClient.Orders.Clear();
    }
}
