using Benzene.SelfHost;
using Microsoft.Extensions.Hosting;

namespace Benzene.HostedService;

public abstract class BenzeneHostedServiceStartup : BenzeneWorkerStartup, IHostedService
{

}
