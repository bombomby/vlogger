using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Components.Models;
using VL.Transport;

namespace VL.Components.Services
{
    public class JobService : Observable
    {
        public List<Job> Jobs { get; set; } = new List<Job>();

        public void Run(JobRequest request)
        {
            lock (Jobs)
            {
                Job job = new Job(request);
                Jobs.Add(job);
                Task.Run(() => job.Run());
            }
            RaisePropertyChanged(nameof(Jobs));
        }

        public void Delete(Job job)
        {
            lock (Jobs)
            {
                Jobs.Remove(job);
            }
            RaisePropertyChanged(nameof(Jobs));
        }

        public void Add(TraceReply trace)
        {
            lock (Jobs)
            {
                foreach (Job job in Jobs)
                {
                    if (job.DB.ProcessID == trace.Process.GroupID)
                    {
                        job.AddTrace(trace);
                        return;
                    }
                }
            }
        }
    }
}
