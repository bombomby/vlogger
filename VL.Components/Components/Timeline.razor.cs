using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Components.Database;
using VL.Components.Models;

namespace VL.Components.Components
{

    public class TimelineVM
    {
        public RectangleF Area;
        public ProcessEntry Process;
        public DateTime Start;
        public DateTime Finish;
        
        public String Cmd => Process.Cmd;
        public String Args => Utils.TrimCommandFromArgs(Process.Args);

        private Color _color;
        public Color Color
        {
            get
            {
                if (_color == Color.Empty)
                    _color = Utils.GetRandomColor(Process.Cmd);
                return _color;
            }
        }

        public bool HasOverlap(TimelineVM other)
        {
            return (Start < other.Start && other.Start < Finish) || (other.Start < Start && Start < other.Finish);
        }
    }

    public class TimelineBase : ComponentBase
    {
        public List<TimelineVM> Items { get; set; } = new List<TimelineVM>();
        public int MaxDepth { get; set; }

        public float RowHeight { get; set; } = 22.0f;
        public float FontSize { get; set; } = 22.0f;
        
        public float ActualWidth { get; set; } = 1211.0f;
        
        public float MaxWidth { get; set; } = 1211.0f;
        public float MaxHeight => MaxDepth * RowHeight;

        protected BufferedUpdate? Ticker { get; set; }

        public void Load(Job job)
        {
            DateTime now = DateTime.Now;

            lock (job.DB)
            {
                JobEntry db = job.DB;

                Items = new List<TimelineVM>();

                foreach (ProcessEntry entry in db.SubProcesses)
                {
                    Items.Add(new TimelineVM()
                    {
                        Process = entry,
                        Start = entry.Start,
                        Finish = entry.IsFinished ? entry.Finish : now,
                    });
                }

                Items.Sort((a, b) => a.Process.Start.CompareTo(b.Process.Start));

                TimeSpan duration = job.IsFinished ? job.Duration : now - job.Start;
                DateTime origin = job.Start;

                //Stack<TimelineVM> currentStack = new Stack<TimelineVM>();

                List<TimelineVM> threads = new List<TimelineVM>();

                foreach (TimelineVM item in Items)
                {
                    int index = -1;
                    for (int i = 0; i < threads.Count; ++i)
                    {
                        if (threads[i].Finish <= item.Start)
                        {
                            threads[i] = item;
                            index = i;
                            break;
                        }
                    }
                    if (index < 0)
                    {
                        index = threads.Count;
                        threads.Add(item);
                    }

                    item.Area = new RectangleF()
                    {
                        X = (float)((item.Start - origin) / duration) * MaxWidth,
                        Y = index * RowHeight,
                        Width = (float)((item.Finish - item.Start) / duration) * MaxWidth,
                        Height = RowHeight,
                    };

                }

                MaxDepth = threads.Count;
            }

            InvokeAsync(() => StateHasChanged());
        }
    }
}
