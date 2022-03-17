using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Net;
using VL.Transport;

namespace VL.Tracer.Services
{
    public class ETWSession
    {
        const int BUFFER_SIZE_MB = 1024;

        TraceEventSession? Session { get; set; }
        Task? SessionWorkerTask { get; set; }

        public bool IsRunning => Session != null;

        class ProcessStorage
        {
            public ulong UniqueProcessKey { get; set; }
            public int ProcessID { get; set; }
            public int GroupID { get; set; }
            public DateTime? Start { get; set; }
            public DateTime? Finish { get; set; }

            public TraceReply? Trace { get; set; }
        }

        Dictionary<ulong, ProcessStorage> AllProcesses = new Dictionary<ulong, ProcessStorage>();
        Dictionary<int, ProcessStorage> ActiveProcesses = new Dictionary<int, ProcessStorage>();

        int RootProcessID { get; set; }

        public async Task StartTraceStream(IServerStreamWriter<TraceReply> responseStream, int updateTimeMs)
        {
            while (true)
            {
                List<TraceReply> queue = new List<TraceReply>();

                lock (AllProcesses)
                {
                    foreach (ProcessStorage process in AllProcesses.Values)
                    {
                        lock (process)
                        {
                            if (process.Trace != null)
                            {
                                TraceReply trace = process.Trace;
                                queue.Add(trace);

                                process.Trace = process.Finish == null ? new TraceReply() { Process = trace.Process } : null;
                            }
                        }
                    }
                }

                foreach (TraceReply trace in queue.OrderBy(t => t.Process.Start))
                {
                    await responseStream.WriteAsync(trace);
                }

                await Task.Delay(updateTimeMs);
            }
        }

        ProcessStorage? GetProcess(int pid)
        {
            ProcessStorage? result = null;
            lock (ActiveProcesses)
            {
                ActiveProcesses.TryGetValue(pid, out result);
            }
            return result;
        }

        public Task<Result> Start(TraceOptions options)
        {
            if (Session == null)
            {
                Session = new TraceEventSession("VL.Tracer");
                Session.BufferSizeMB = BUFFER_SIZE_MB;

                KernelTraceEventParser.Keywords settings = KernelTraceEventParser.Keywords.Process;

                if (options.Network)
                    settings |= KernelTraceEventParser.Keywords.NetworkTCPIP;

                if (options.FileIO)
                    settings |= KernelTraceEventParser.Keywords.FileIO | KernelTraceEventParser.Keywords.FileIOInit;

                try
                {
                    Session.EnableKernelProvider(settings);

                    // Processes
                    Session.Source.Kernel.ProcessStart += Kernel_ProcessStart;
                    Session.Source.Kernel.ProcessStop += Kernel_ProcessStop;

                    // Network
                    Session.Source.Kernel.UdpIpSend += Kernel_UdpIpSend;
                    Session.Source.Kernel.UdpIpRecv += Kernel_UdpIpRecv;
                    Session.Source.Kernel.UdpIpSendIPV6 += Kernel_UdpIpSendIPV6;
                    Session.Source.Kernel.UdpIpRecvIPV6 += Kernel_UdpIpRecvIPV6;
                    Session.Source.Kernel.TcpIpSend += Kernel_TcpIpSend;
                    Session.Source.Kernel.TcpIpRecv += Kernel_TcpIpRecv;
                    Session.Source.Kernel.TcpIpRecvIPV6 += Kernel_TcpIpRecvIPV6;
                    Session.Source.Kernel.TcpIpSendIPV6 += Kernel_TcpIpSendIPV6;

                    // FileIO
                    Session.Source.Kernel.FileIORead += Kernel_FileIORead;
                    Session.Source.Kernel.FileIOWrite += Kernel_FileIOWrite;
                    Session.Source.Kernel.FileIODelete += Kernel_FileIODelete;

                    RootProcessID = options.ProcessID;

                    SessionWorkerTask = Task.Run(() => Session.Source.Process());
                }
                catch (Exception ex)
                {
                    Session.Dispose();
                    Session = null;
                    RootProcessID = 0;
                    return Res.Error(ex.Message);
                }
            }
            return Res.Ok();
        }

        private void Kernel_ProcessStart(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ProcessTraceData obj)
        {
            ProcessStorage? parent = GetProcess(obj.ParentID);
            if (parent == null && RootProcessID != obj.ParentID)
                    return;

            ProcessStorage process = new ProcessStorage()
            {
                UniqueProcessKey = obj.UniqueProcessKey,
                ProcessID = obj.ProcessID,
                GroupID = parent != null ? parent.GroupID : obj.ProcessID,
                Start = obj.TimeStamp,
                Trace = new TraceReply(),
            };

            process.Trace.Process = new ProcessDescription()
            {
                Application = obj.ProcessName,
                Args = obj.CommandLine,
                ParentUniqueProcessKey = parent != null ? parent.UniqueProcessKey : 0,
                ParentID = obj.ParentID,
                ProcessID = obj.ProcessID,
                GroupID = process.GroupID,
                Start = Timestamp.FromDateTime(obj.TimeStamp.ToUniversalTime()),
                UniqueProcessKey = obj.UniqueProcessKey,
            };

            lock (AllProcesses)
            {
                AllProcesses[obj.UniqueProcessKey] = process;
            }

            lock (ActiveProcesses)
            {
                ActiveProcesses.Add(process.ProcessID, process);
            }
        }

        private void Kernel_ProcessStop(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ProcessTraceData obj)
        {
            ProcessStorage? process = GetProcess(obj.ProcessID);
            if (process != null)
            {
                lock (process)
                {
                    process.Finish = obj.TimeStamp;

                    if (process.Trace != null)
                    {
                        lock (process.Trace)
                        {
                            process.Trace.Process.Finish = Timestamp.FromDateTime(obj.TimeStamp.ToUniversalTime());
                        }
                    }
                }

                lock (ActiveProcesses)
                {
                    ActiveProcesses.Remove(process.ProcessID);
                }
            }
        }

        private void OnFileIOMsg(int processId, string filename, int size, DateTime time, FileTrace.Types.Command cmd)
        {
            ProcessStorage? process = GetProcess(processId);
            if (process != null)
            {
                lock (process)
                {
                    FileTrace trace = new FileTrace()
                    {
                        Filename = filename,
                        Size = size,
                        Timestamp = Timestamp.FromDateTime(time.ToUniversalTime()),
                        Cmd = cmd,
                    };
                    process.Trace!.FileIO.Add(trace);
                }
            }
        }

        private void OnNetworkMsg(int processId, IPAddress address, int size, DateTime time, NetworkTrace.Types.Command cmd)
        {
            ProcessStorage? process = GetProcess(processId);
            if (process != null)
            {
                lock (process)
                {
                    NetworkTrace trace = new NetworkTrace()
                    {
                        Address = address.ToString(),
                        Size = size,
                        Timestamp = Timestamp.FromDateTime(time.ToUniversalTime()),
                        Cmd = cmd,
                    };
                    process.Trace!.Network.Add(trace);
                }
            }
        }

        private void Kernel_UdpIpRecvIPV6(Microsoft.Diagnostics.Tracing.Parsers.Kernel.UpdIpV6TraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Receive);
        }

        private void Kernel_UdpIpSendIPV6(Microsoft.Diagnostics.Tracing.Parsers.Kernel.UpdIpV6TraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Send);
        }

        private void Kernel_UdpIpRecv(Microsoft.Diagnostics.Tracing.Parsers.Kernel.UdpIpTraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Receive);
        }

        private void Kernel_UdpIpSend(Microsoft.Diagnostics.Tracing.Parsers.Kernel.UdpIpTraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Send);
        }

        private void Kernel_TcpIpRecv(Microsoft.Diagnostics.Tracing.Parsers.Kernel.TcpIpTraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Receive);
        }

        private void Kernel_TcpIpSend(Microsoft.Diagnostics.Tracing.Parsers.Kernel.TcpIpSendTraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Send);
        }

        private void Kernel_TcpIpSendIPV6(Microsoft.Diagnostics.Tracing.Parsers.Kernel.TcpIpV6SendTraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Send);
        }

        private void Kernel_TcpIpRecvIPV6(Microsoft.Diagnostics.Tracing.Parsers.Kernel.TcpIpV6TraceData obj)
        {
            OnNetworkMsg(obj.ProcessID, obj.daddr, obj.size, obj.TimeStamp, NetworkTrace.Types.Command.Receive);
        }

        private void Kernel_FileIOWrite(Microsoft.Diagnostics.Tracing.Parsers.Kernel.FileIOReadWriteTraceData obj)
        {
            OnFileIOMsg(obj.ProcessID, obj.FileName, obj.IoSize, obj.TimeStamp, FileTrace.Types.Command.Write);
        }

        private void Kernel_FileIORead(Microsoft.Diagnostics.Tracing.Parsers.Kernel.FileIOReadWriteTraceData obj)
        {
            OnFileIOMsg(obj.ProcessID, obj.FileName, obj.IoSize, obj.TimeStamp, FileTrace.Types.Command.Read);
        }

        private void Kernel_FileIODelete(Microsoft.Diagnostics.Tracing.Parsers.Kernel.FileIOInfoTraceData obj)
        {
            OnFileIOMsg(obj.ProcessID, obj.FileName, 0, obj.TimeStamp, FileTrace.Types.Command.Delete);
        }

        public Task<Result> Stop()
        {
            if (Session != null)
            {
                Session.Stop();

                if (SessionWorkerTask != null && !SessionWorkerTask.IsCompleted)
                    SessionWorkerTask.Wait();

                SessionWorkerTask = null;

                Session.Dispose();
                Session = null;

                lock (ActiveProcesses)
                {
                    ActiveProcesses.Clear();
                }

                return Res.Ok();
            }
            return Res.Warning("ETW Session is not running!");
        }
    }
}
