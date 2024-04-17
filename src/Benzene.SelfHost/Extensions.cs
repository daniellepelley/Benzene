namespace Benzene.SelfHost;

public static class Extensions
{
    public static BenzeneHost BuildHost(this InlineSelfHostedStartUp source)
    {
        return new BenzeneHost(source.Build());
    }
}
