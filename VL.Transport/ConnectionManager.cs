using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace VL.Transport
{
    public class ConnectionManager
    {
        ILogger<ConnectionManager> _logger;

        public const string PipeName = "VisualLoggerPipe";

        public bool IsConnected { get; set; }

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        public void StartServer()
        {
            Task.Run(() => UpdateConnection());
        }

        private void UpdateConnection()
        {
            while (true)
            {
                using (NamedPipeServerStream pipe = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, CreateSystemIOPipeSecurity()))
                {
                    pipe.WaitForConnection();
                    IsConnected = true;
                    _logger.LogInformation($"Connection is established!");
                    try
                    {
                        using (StreamReader reader = new StreamReader(pipe))
                            reader.ReadToEnd();
                    }
                    finally
                    {
                        IsConnected = false;
                        _logger.LogInformation("Connection is lost!");
                    }
                }
            }
        }

        static PipeSecurity CreateSystemIOPipeSecurity()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            var id = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            pipeSecurity.SetAccessRule(new PipeAccessRule(id, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            return pipeSecurity;
        }
    }

    public class ConnectionClient
    {
        ILogger _logger;

        public bool IsConnected { get; set; }

        public ConnectionClient(ILogger logger)
        {
            _logger = logger;
        }

        public void StartClient()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", ConnectionManager.PipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
                    {
                        pipe.Connect();
                        IsConnected = true;
                        _logger.LogInformation("Connected is established!");
                        try
                        {
                            using (StreamReader reader = new StreamReader(pipe))
                                reader.ReadToEnd();
                        }
                        finally
                        {
                            IsConnected = false;
                            _logger.LogInformation("Connection is lost!");
                        }
                    }
                }
            });
        }
    }


}
