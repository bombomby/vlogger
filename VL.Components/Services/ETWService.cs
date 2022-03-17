using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using VL.Components.Models;
using VL.Transport;

namespace VL.Components.Services
{
    public class ETWService : Observable
    {
        GrpcChannel Channel { get; set; }
        public ETWTracer.ETWTracerClient Client { get; set; }
        
        Task? StreamingTask { get; set; }

        private readonly ILogger<ETWService> _logger;
        private readonly JobService _jobService;

        Process? TracerProcess { get; set; }
        ConnectionClient ConnectionMonitoringClient;

        public ETWService(ILogger<ETWService> logger, JobService jobService)
        {
            _logger = logger;
            _jobService = jobService;

            Channel = GrpcChannel.ForAddress($"http://localhost:{ConnectionSettings.Port}");
            Client = new ETWTracer.ETWTracerClient(Channel);

            ConnectionMonitoringClient = new ConnectionClient(logger);
            ConnectionMonitoringClient.StartClient();
        }

        private async Task<bool> EnsureServerIsRunning(float timeoutSec)
        {
            if (!ConnectionMonitoringClient.IsConnected)
            {
                if (TracerProcess == null || TracerProcess.HasExited)
                {
                    string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    TracerProcess = new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = Path.Combine(dir!, "VL.Tracer.exe"),
                            UseShellExecute = true,
                        }
                    };
                    TracerProcess.Start();

                    DateTime finish = DateTime.Now + TimeSpan.FromSeconds(timeoutSec);
                    while (DateTime.Now < finish && !ConnectionMonitoringClient.IsConnected)
                    {
                        await Task.Delay(1000);
                    }
                }
            }
            return ConnectionMonitoringClient.IsConnected;
        }

        private void TracerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            _logger.LogError($"VL.Tracer.exe: {e.Data}");
        }

        private void TracerProcerr_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            _logger.LogInformation($"VL.Tracer.exe: {e.Data}");
        }

        

        async Task StartStreaming()
        {
            try
            {
                IsTracingActive = true;
                using (var call = Client.StartTraceStream(new Empty()))
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        TraceReply reply = call.ResponseStream.Current;
                        _jobService.Add(reply);
                    }
                }
            }
            catch (RpcException ex)
            {
                _logger.LogError($"Failed to start streaming trace data: {ex.Message}");
            }
            finally
            {
                IsTracingActive = false;
            }
        }

        public async Task<Result> EnableTracing(bool enabled)
        {
            Result res;
            if (enabled)
            {
                await EnsureServerIsRunning(10000);

                res = await Client.EnableTracingAsync(new Transport.TraceOptions { ProcessID = Process.GetCurrentProcess().Id, Network = true, FileIO = true });
                if (res.Code != Result.Types.ErrorCode.Error)
                    StreamingTask = Task.Run(async () => await StartStreaming());
            }
            else
            {
                //StreamingTask.Wait();
                res = await Client.DisableTracingAsync(new Empty());
            }

            return res;
        }

        public async Task<bool> IsTracingActiveOnService()
        {
            try
            {
                BoolValue res = await Client.IsTracingActiveAsync(new Empty());
                return res.Value;
            }
            catch (RpcException) { }
            return false;
        }

        public bool IsTracingActive { get; set; }
        public bool IsConnected => ConnectionMonitoringClient.IsConnected;
    }
}
