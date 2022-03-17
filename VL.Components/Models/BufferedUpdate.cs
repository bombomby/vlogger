using System;

namespace VL.Components.Models
{
    public class BufferedUpdate : IDisposable
    {
        System.Timers.Timer CountdownTimer { get; set; }
        Action Callback { get; set; }
        bool IsRunning { get; set; }

        public BufferedUpdate(Action callback, float timeSec = 1.0f)
        {
            Callback = callback;
            CountdownTimer = new System.Timers.Timer(timeSec * 1000.0);
            CountdownTimer.Elapsed += CountdownTimer_Elapsed;
            CountdownTimer.Start();
        }

        private void CountdownTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Callback();
        }

        public void RequestUpdate()
        {
            if (!CountdownTimer.Enabled)
                CountdownTimer.Start();
        }

        public void Dispose()
        {
            CountdownTimer.Stop();
            CountdownTimer.Elapsed -= CountdownTimer_Elapsed;
        }
    }
}
