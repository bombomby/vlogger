using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VL.Components.Database;
using VL.Components.Services;
using VL.Transport;

namespace VL.Components.Models
{
    public class JobRequest
    {
        public String Command { get; set; }
        public String Args { get; set; }

        public JobRequest(String command, String args)
        {
            Command = command;
            Args = args;
        }
    }

    public class FileIOSummary
    {
        public class Entry
        {
            public String Filename { get; set; }
            public Int64 Read { get; set; }
            public Int64 Write { get; set; }
            public Int64 Total => Read + Write;
            public Entry(String filename)
            {
                Filename = filename;
            }
        }
        public Dictionary<String, Entry> Entries { get; set; } = new Dictionary<string, Entry>();

        public void AddRange(List<FileEntry> fileStats)
        {
            foreach (FileEntry fileEntry in fileStats)
            {
                Entry? entry = null;
                if (!Entries.TryGetValue(fileEntry.Filename, out entry))
                {
                    entry = new Entry(fileEntry.Filename);
                    Entries.Add(fileEntry.Filename, entry);
                }

                switch (fileEntry.Cmd)
                {
                    case FileTrace.Types.Command.Read:
                        entry.Read = entry.Read + fileEntry.Size;
                        break;


                    case FileTrace.Types.Command.Write:
                        entry.Write = entry.Write + fileEntry.Size;
                        break;                
                }
            }
        }
    }

    public class NetworkSummary
    {
        public class Entry
        {
            public String Address { get; set; }
            public String? Hostname { get; set; }
            public Int64 Send { get; set; }
            public Int64 Receive { get; set; }
            public Int64 Total => Send + Receive;
            public Entry(String address)
            {
                Address = address;
            }
        }
        public Dictionary<String, Entry> Entries { get; set; } = new Dictionary<string, Entry>();

        string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch (SocketException)
            {
                //not every IP has a name
            }
            return ipAddress;
        }

        public void AddRange(List<NetworkEntry> networkStats)
        {
            foreach (NetworkEntry networkEntry in networkStats)
            {
                Entry? entry = null;
                if (!Entries.TryGetValue(networkEntry.Address, out entry))
                {
                    entry = new Entry(networkEntry.Address);
                    Task.Run(() => entry.Hostname = GetHostName(entry.Address));
                    Entries.Add(networkEntry.Address, entry);
                }

                switch (networkEntry.Cmd)
                {
                    case NetworkTrace.Types.Command.Send:
                        entry.Send += networkEntry.Size;
                        break;


                    case NetworkTrace.Types.Command.Receive:
                        entry.Receive += networkEntry.Size;
                        break;
                }
            }
        }
    }

    public class Job : Observable
    {
        public JobEntry DB { get; set; }
        
        //public List<LogEntry> Log { get; set; } = new List<LogEntry>();

        public String Cmd => DB.Cmd;
        public String Args => DB.Args;
        public TimeSpan Duration => DB.Finish - DB.Start;
        public DateTime Start => DB.Start;
        public DateTime Finish => DB.Finish;
        public int ExitCode => DB.ExitCode;
        public bool IsFinished => DB.IsFinished;

        CountdownEvent PendingSubProcesses = new CountdownEvent(1);

        public Job(JobRequest request) 
            : this(new JobEntry() {
                    Id = (ulong)new Random().NextInt64(),
                    Cmd = request.Command,
                    Args = request.Args,
                })
        {
        }

        public Job(JobEntry dbEntry)
        {
            DB = dbEntry;
            DB.PropertyChanged += DB_PropertyChanged;
        }

        private FileIOSummary? _fileSummary;
        public FileIOSummary FileSummary
        {
            get
            {
                if (_fileSummary == null)
                {
                    _fileSummary = new FileIOSummary();
                    lock (DB)
                    {
                        _fileSummary.AddRange(DB.FileStats);
                    }
                }
                return _fileSummary;
            }
        }

        private NetworkSummary? _networkSummary;
        public NetworkSummary NetworkSummary
        {
            get
            {
                if (_networkSummary == null)
                {
                    _networkSummary = new NetworkSummary();
                    lock (DB)
                    {
                        _networkSummary.AddRange(DB.NetworkStats);
                    }
                }
                return _networkSummary;
            }
        }

        private void DB_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("DB");
        }

        public void Run()
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = DB.Cmd,
                    Arguments = DB.Args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            })
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    lock (DB)
                    {
                        DB.Logs.Add(new LogEntry() { Message = args.Data ?? "<None>", Timestamp = DateTime.Now, Channel = LogChannel.Info });
                        DB.OutputCount = DB.OutputCount + 1;
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    lock (DB)
                    {
                        DB.Logs.Add(new LogEntry() { Message = args.Data ?? "<None>", Timestamp = DateTime.Now, Channel = LogChannel.Error });
                        DB.ErrorCount = DB.ErrorCount + 1;
                    }
                };

                DB.Start = DateTime.Now;
                
                process.Start();
                
                DB.ProcessID = process.Id;

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                DB.ExitCode = process.ExitCode;

                DB.Finish = DateTime.Now;
                DB.IsFinished = true;

                //PendingSubProcesses.Signal();

                Task.Run(() =>
                {
                    // Sleeping for 5 seconds (in case tracing service is active before saving it)
                    Task.Delay(5000).Wait();
                    Save();
                });
            }
        }

        public void AddTrace(TraceReply trace)
        {
            lock (DB)
            {
                ProcessEntry? process = DB.SubProcesses.Find(p => p.Id == trace.Process.UniqueProcessKey);
                if (process == null)
                {
                    process = new ProcessEntry()
                    {
                        Id = trace.Process.UniqueProcessKey,
                        Args = trace.Process.Args,
                        Cmd = trace.Process.Application,
                        Start = trace.Process.Start.ToDateTime(),
                        ParentProcessID = trace.Process.ParentID,
                        ParentUniqueId = trace.Process.ParentUniqueProcessKey,
                        ProcessID = trace.Process.ProcessID,
                    };
                    DB.NumSubProcesses += 1;
                    DB.SubProcesses.Add(process);
                    //PendingSubProcesses.AddCount();
                }

                Int64 send = 0;
                Int64 receive = 0;

                foreach (NetworkTrace entry in trace.Network)
                {
                    NetworkEntry networkEntry = new NetworkEntry()
                    {
                        Address = entry.Address,
                        Cmd = entry.Cmd,
                        Process = process!,
                        Size = entry.Size,
                        Timestamp = entry.Timestamp.ToDateTime(),
                    };
                    DB.NetworkStats.Add(networkEntry);

                    switch (entry.Cmd)
                    {
                        case NetworkTrace.Types.Command.Send:
                            send += entry.Size;
                            break;

                        case NetworkTrace.Types.Command.Receive:
                            receive += entry.Size;
                            break;
                    }

                    if (_networkSummary != null)
                        _networkSummary.AddRange(new List<NetworkEntry>() { networkEntry });
                }

                Int64 read = 0;
                Int64 write = 0;

                foreach (FileTrace entry in trace.FileIO)
                {
                    FileEntry fileEntry = new FileEntry()
                    {
                        Cmd = entry.Cmd,
                        Filename = entry.Filename,
                        Process = process!,
                        Size = entry.Size,
                        Timestamp = entry.Timestamp.ToDateTime(),
                    };

                    DB.FileStats.Add(fileEntry);

                    switch (entry.Cmd)
                    {
                        case FileTrace.Types.Command.Read:
                            read += entry.Size;
                            break;

                        case FileTrace.Types.Command.Write:
                            write += entry.Size;
                            break;
                    }

                    if (_fileSummary != null)
                        _fileSummary.AddRange(new List<FileEntry> { fileEntry });
                }

                DB.NetworkSend = DB.NetworkSend + send;
                DB.NetworkReceive = DB.NetworkReceive + receive;

                DB.FileRead = DB.FileRead + read;
                DB.FileWrite = DB.FileWrite + write;

                if (trace.Process.Finish != null)
                {
                    process!.Finish = trace.Process.Finish.ToDateTime();
                    //PendingSubProcesses.Signal();
                }
            }
        }

        public void Save()
        {
            //PendingSubProcesses.Wait();

            lock (DB)
            {
                using (JobContext db = new JobContext())
                {
                    db.Database.EnsureCreated();
                    db.Jobs.Add(DB);
                    db.SaveChangesAsync();
                }
            }
        }
    }
}
