using Benzene.Diagnostics.Timers;
using zipkin4net;

namespace Benzene.Zipkin;

public sealed class ZipkinProcessTimer : IProcessTimer
{
    private readonly Trace? _trace;

    public ZipkinProcessTimer(string timerName)
    {
        Trace.Current = Trace.Current.Child();
        _trace = Trace.Current;
         _trace.Record(Annotations.ServiceName("benzene"));
         _trace.Record(Annotations.Rpc(timerName));
        _trace.Record(Annotations.LocalOperationStart("middleware"));
    }
    public void Dispose()
    {
         _trace.Record(Annotations.LocalOperationStop());
    }
    
    public void SetTag(string key, string value)
    {
        _trace.Record(Annotations.Tag(key, value));
    }
}