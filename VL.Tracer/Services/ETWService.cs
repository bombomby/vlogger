using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using VL.Transport;

namespace VL.Tracer.Services
{
    public class ETWService : ETWTracer.ETWTracerBase
    {
        public int UpdateTimeMs { get; set; } = 1000;
        
        private readonly ETWSession _session;
        private readonly ILogger<ETWService> _logger;

        public ETWService(ILogger<ETWService> logger, ETWSession session)
        {
            _logger = logger;
            _session = session;
        }

        public override Task<Result> EnableTracing(TraceOptions request, ServerCallContext context)
        {
            return _session.Start(request);
        }

        public override Task<Result> DisableTracing(Empty request, ServerCallContext context)
        {
            return _session.Stop();
        }

        public override async Task StartTraceStream(Empty request, IServerStreamWriter<TraceReply> responseStream, ServerCallContext context)
        {
            await _session.StartTraceStream(responseStream, UpdateTimeMs);
        }

        public override Task<BoolValue> IsTracingActive(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new BoolValue() { Value = _session.IsRunning });
        }
    }
}