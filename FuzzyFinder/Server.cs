using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.IO;

namespace PipeServer
{
    class Server
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateNamedPipe(
           String pipeName,
           uint dwOpenMode,
           uint dwPipeMode,
           uint nMaxInstances,
           uint nOutBufferSize,
           uint nInBufferSize,
           uint nDefaultTimeOut,
           IntPtr lpSecurityAttributes);

        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(uint nStdHandle);

        public const uint STD_OUTPUT_HANDLE = 4294967285;
        public const uint CREATE_NO_WINDOW = 0x08000000;
        public const uint DUPLEX = (0x00000003);
        public const uint FILE_FLAG_OVERLAPPED = (0x40000000);

        public class Client
        {
            public SafeFileHandle handle;
            public FileStream stream;
        }

        public delegate void MessageReceivedHandler(Client client, string message);

        public const int BUFFER_SIZE = 4096;

        string pipeName;
        string directory;
        bool running;
        Client client;

        public string PipeName
        {
            get { return this.pipeName; }
            set { this.pipeName = value; }
        }

        public bool Running
        {
            get { return this.running; }
        }

        public string Directory
        {
            get { return this.directory; }
        }

        public Server()
        {
            pipeName = "\\\\.\\pipe\\fuzzy";
            this.client = new Client();
            this.directory = System.IO.Directory.GetCurrentDirectory();
            Start();
            ListenForClients();
        }

        /// <summary>
        /// Starts the pipe server
        /// </summary>
        public void Start()
        {
            //start the listening thread
            this.running = true;
        }

        /// <summary>
        /// Listens for client connections
        /// </summary>
        private void ListenForClients()
        {
            SafeFileHandle clientHandle =
            CreateNamedPipe(
                 this.pipeName,
                 DUPLEX,
                 0,
                 255,
                 BUFFER_SIZE,
                 BUFFER_SIZE,
                 5000,
                 IntPtr.Zero);

            //could not create named pipe
            if (clientHandle.IsInvalid)
                return;

            // Create the child process.
            STARTUPINFO si = new STARTUPINFO();
            si.cb = (uint)Marshal.SizeOf(typeof(STARTUPINFO));
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            string appPath = "ruby -- \"";
            appPath += Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            appPath += "\\fuzz.rb\"";

            bool result = CreateProcess(
                    null,
                    appPath,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    true,
                    CREATE_NO_WINDOW,
                    IntPtr.Zero,
                    null,
                    ref si,
                    out pi);
            if (!result)
            {
                return;
            }

            client.handle = clientHandle;
            client.stream = new FileStream(client.handle, FileAccess.ReadWrite, BUFFER_SIZE, false);
        }

        /// <summary>
        /// Reads incoming data from connected clients
        /// </summary>
        /// <param name="clientObj"></param>
        private string Read(object clientObj)
        {
            string outStr = "";
            byte[] buffer = new byte[BUFFER_SIZE];
            ASCIIEncoding encoder = new ASCIIEncoding();

            while (true)
            {
                int bytesRead = 0;

                try
                {
                    bytesRead = client.stream.Read(buffer, 0, BUFFER_SIZE);
                }
                catch
                {
                    //read error has occurred
                    break;
                }

                if (bytesRead == 0)
                    break;

                outStr += encoder.GetString(buffer, 0, bytesRead);
                
                if (bytesRead < BUFFER_SIZE) {
                    break;
                }
            }
            return outStr;
        }

        /// <summary>
        /// Sends a message to all connected clients
        /// </summary>
        /// <param name="message">the message to send</param>
        public void SendMessage(string message)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] messageBuffer = encoder.GetBytes(message);
            client.stream.Write(messageBuffer, 0, messageBuffer.Length);

            try
            {
                client.stream.Flush();
            }
            catch (Exception)
            {
                // Ignore exception because client probably hasn't connected.
            }
        }

        public string RunFinder(String part)
        {
            SendMessage(part);
            return Read(client);
        }
    }
}
