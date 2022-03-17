using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Components.Models;
using VL.Transport;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace VL.Components.Database
{
    public class ProcessEntry : Observable
    {
        public ulong Id { get; set; }

        public string Cmd { get; set; }
        public string Args { get; set; }
        
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        
        public ulong ParentUniqueId { get; set; }

        public int ProcessID { get; set; }
        public int ParentProcessID { get; set; }

        [NotMapped]
        public bool IsFinished => Finish != null && Finish >= Start;
    }

    public class JobEntry : Observable
    {
        public ulong Id { get; set; }

        public string Cmd { get; set; }
        public string Args { get; set; }

        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }

        public int ProcessID { get; set; }

        public int ExitCode { get; set; }
        public int OutputCount { get; set; }
        public int ErrorCount { get; set; }
        public bool IsFinished { get; set; }

        #region ETW Section (Available only if VL.Tracer is enabled)
        public Int64 NumSubProcesses { get; set; }

        public Int64 NetworkSend { get; set; }
        public Int64 NetworkReceive { get; set; }

        public Int64 FileRead { get; set; }
        public Int64 FileWrite { get; set; }

        public virtual List<LogEntry> Logs { get; set; } = new List<LogEntry>();
        public virtual List<NetworkEntry> NetworkStats { get; set; } = new List<NetworkEntry>();
        public virtual List<FileEntry> FileStats { get; set; } = new List<FileEntry>();
        public virtual List<ProcessEntry> SubProcesses { get; set; } = new List<ProcessEntry>();
        #endregion
    }

    public enum LogChannel
    {
        Info,
        Error,
    }

    public class SubProcessEntry
    {

    }

    public class LogEntry
    {
        public ulong Id { get; set; }
        public LogChannel Channel { get; set; }
        public DateTime Timestamp { get; set; }
        public String Message { get; set; }

        public ulong JobId { get; set; }

        public LogEntry()
        {
            Id = (ulong)new Random().NextInt64();
        }
    }

    public class NetworkEntry
    {
        public ulong Id { get; set; }
        public DateTime Timestamp { get; set; }
        public String Address { get; set; }
        public Int64 Size { get; set; }
        public NetworkTrace.Types.Command Cmd { get; set; }

        virtual public ProcessEntry Process { get; set; }

        public NetworkEntry()
        {
            Id = (ulong)new Random().NextInt64();
        }
    }

    public class FileEntry
    {
        public ulong Id { get; set; }
        public DateTime Timestamp { get; set; }
        public String Filename { get; set; }
        public Int64 Size { get; set; }
        public FileTrace.Types.Command Cmd { get; set; }

        virtual public ProcessEntry Process { get; set; }

        public FileEntry()
        {
            Id = (ulong)new Random().NextInt64();
        }
    }

    public class JobContext : DbContext
    {
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<JobEntry> Jobs { get; set; }

        public static String DbName = "VLogger";
        public static String DbPath { get; set; }

        static JobContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, $"{DbName}.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseLazyLoadingProxies().UseSqlite($"Data Source={DbPath}");
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
