using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Interop.Kernel32;

namespace Interop
{
    public class Process : IDisposable
    {
        STARTUPINFO StartupInfo;
        PROCESS_INFORMATION ProcessInfo;

        CreateProcessFlags Flags = CreateProcessFlags.CREATE_DEFAULT_ERROR_MODE;

        public String ApplicationName { get; private set; }
        public String Args { get; private set; }
        public uint ExitCode { get; private set; }
        public int PID => ProcessInfo.dwProcessId;

        public Process(string appName, string appArgs)
        {
            ApplicationName = appName;
            Args = appArgs;
        }

        public void StartSuspended()
        {
            Flags |= CreateProcessFlags.CREATE_SUSPENDED;
            Start();
        }

        enum PipeMode
        {
            IN,
            OUT,
            ERR,
        }

        class Pipe : IDisposable
        {
            public PipeMode Mode;
            public IntPtr hRead;
            public IntPtr hWrite;

            public FileStream SendStream;
            public FileStream ReceiveStream;

            public Pipe(PipeMode mode)
            {
                Mode = mode;

                int securityAttributeSize = Marshal.SizeOf<SECURITY_ATTRIBUTES>();
                var pipeSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize, bInheritHandle = 1 };

                if (!CreatePipe(out hRead, out hWrite, ref pipeSec, 0))
                    throw new InvalidOperationException("CreatePipe failed: " + Marshal.GetLastWin32Error());

                if (!SetHandleInformation(hRead, HANDLE_FLAG_INHERIT, 0))
                    throw new InvalidOperationException("SetHandleInformation failed: " + Marshal.GetLastWin32Error());

                SendStream = new FileStream(new SafeFileHandle(hRead, true), FileAccess.Write);
                ReceiveStream = new FileStream(new SafeFileHandle(hWrite, true), FileAccess.Read);
            }

            public void Dispose()
            {
                SendStream.Close();
                ReceiveStream.Close();
                CloseHandle(hRead);
                CloseHandle(hWrite);
                hRead = IntPtr.Zero;
                hWrite = IntPtr.Zero;
            }
        }


        List<Pipe> Pipes = new List<Pipe>();
        Pipe GetPipe(PipeMode mode)
        {
            return Pipes.First(p => p.Mode == mode);
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/procthread/creating-a-child-process-with-redirected-input-and-output
        /// </summary>
        public void Start()
        {
            Pipes.AddRange(new Pipe[]
            {
                new Pipe(PipeMode.IN),
                new Pipe(PipeMode.OUT),
                new Pipe(PipeMode.ERR)
            });
           
            StartupInfo = new STARTUPINFO();
            StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

            // Setup STDIN\STDOUT\STDERR redirection
            StartupInfo.hStdInput = GetPipe(PipeMode.IN).hRead;
            StartupInfo.hStdOutput = GetPipe(PipeMode.OUT).hWrite;
            StartupInfo.hStdError = GetPipe(PipeMode.ERR).hWrite;
            StartupInfo.dwFlags = (int)(StartupInfoFlags.STARTF_USESTDHANDLES);

            ProcessInfo = new PROCESS_INFORMATION();

            int securityAttributeSize = Marshal.SizeOf<SECURITY_ATTRIBUTES>();
            var pSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var tSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };

            var success = CreateProcess(
                lpApplicationName: null!,
                lpCommandLine: $"\"{ApplicationName}\" {Args}",
                lpProcessAttributes: ref pSec,
                lpThreadAttributes: ref tSec,
                bInheritHandles: true,
                dwCreationFlags: (uint)Flags,
                lpEnvironment: IntPtr.Zero,
                lpCurrentDirectory: null!,
                lpStartupInfo: ref StartupInfo,
                lpProcessInformation: out ProcessInfo
            );
            if (!success)
            {
                Dispose();
                throw new InvalidOperationException("Could not create process. " + Marshal.GetLastWin32Error());
            }
        }

        public bool Resume()
        {
            var result = Kernel32.ResumeThread(ProcessInfo.hThread);
            return result != -1;
        }

        public void Wait()
        {
            Kernel32.WaitForSingleObject(ProcessInfo.hProcess, 0xFFFFFFFF);

            uint exitCode = 0;
            Kernel32.GetExitCodeProcess(ProcessInfo.hProcess, out exitCode);
            ExitCode = exitCode;
        }

        public void Dispose()
        {
            Kernel32.CloseHandle(ProcessInfo.hProcess);
            Kernel32.CloseHandle(ProcessInfo.hThread);
            Pipes.ForEach(p => p.Dispose());
            Pipes.Clear();
        }
    }
}
