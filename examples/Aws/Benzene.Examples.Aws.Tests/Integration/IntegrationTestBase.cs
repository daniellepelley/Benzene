using Benzene.Aws.Lambda.Core;
using Benzene.Examples.App.Data;
using Benzene.Examples.Aws.Tests.Helpers;
using Benzene.Tools.Aws;

namespace Benzene.Examples.Aws.Tests.Integration;

public abstract class InMemoryOrdersTestBase
{
    protected AwsLambdaBenzeneTestHost TestLambdaHosting;

    protected InMemoryOrdersTestBase(IAwsLambdaEntryPoint lambdaEntryPoint)
    {
        SetUp();
        TestLambdaHosting = new AwsLambdaBenzeneTestHost(lambdaEntryPoint);
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
