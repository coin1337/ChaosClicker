using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

class AutoClicker
{
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    const int VK_ESCAPE = 0x1B;
    const int VK_F1 = 0x70;
    const int VK_F2 = 0x71;
    const int VK_F3 = 0x72;
    const int VK_F4 = 0x73;
    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    struct POINT
    {
        public int X;
        public int Y;
    }

    static volatile bool running = true;
    static volatile bool autoclickerActive = true;
    static volatile bool programHidden = false;
    static volatile bool specificProcessOnly = false;
    static volatile int clicksPerSecond = 10;
    static volatile ConsoleKey toggleKey = ConsoleKey.F1;
    static volatile ConsoleKey adjustKey = ConsoleKey.F2;
    static volatile ConsoleKey hideKey = ConsoleKey.F3;
    static volatile ConsoleKey processSelectKey = ConsoleKey.F4;
    static volatile Process targetProcess = null;

    static void Main(string[] args)
    {
        string appName = GenerateRandomString(8);

        Console.Title = appName;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
   _____ _                      _____ _ _      _             
  / ____| |                    / ____| (_)    | |            
 | |    | |__   __ _  ___  ___| |    | |_  ___| | _____ _ __ 
 | |    | '_ \ / _` |/ _ \/ __| |    | | |/ __| |/ / _ \ '__|
 | |____| | | | (_| | (_) \__ \ |____| | | (__|   <  __/ |   
  \_____|_| |_|\__,_|\___/|___/\_____|_|_|\___|_|\_\___|_|   
                                                             
");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("coded by wealthy#0227");
        Console.ResetColor();

        Console.WriteLine( );
        Console.WriteLine("enter the initial clicks per second (1-100):");

        while (true)
        {
            if (int.TryParse(Console.ReadLine(), out clicksPerSecond) && clicksPerSecond >= 1 && clicksPerSecond <= 100)
            {
                break;
            }
            Console.WriteLine("invalid input. please enter a number between 1 and 100.");
        }

        Thread autoclickerThread = new Thread(AutoClick);
        autoclickerThread.Start();

        Console.WriteLine("autoclicker started.");

        ShowMenu();

        int previousClicksPerSecond = clicksPerSecond;
        List<Process> processes = new List<Process>();

        while (running)
        {
            if (GetAsyncKeyState((int)toggleKey) != 0)
            {
                autoclickerActive = !autoclickerActive;

                if (autoclickerActive)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("autoclicker activated.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("autoclicker paused.");
                }

                Console.ResetColor();
                Thread.Sleep(200);
                ShowMenu();
            }

            if (GetAsyncKeyState((int)adjustKey) != 0)
            {
                Console.WriteLine("enter new clicks per second (1-100):");
                int newClicksPerSecond;
                if (int.TryParse(Console.ReadLine(), out newClicksPerSecond) && newClicksPerSecond >= 1 && newClicksPerSecond <= 100)
                {
                    clicksPerSecond = newClicksPerSecond;
                    Console.WriteLine("clicks per second adjusted to: " + clicksPerSecond);
                }
                else
                {
                    Console.WriteLine("invalid input. clicks per second unchanged.");
                }

                ShowMenu();
            }

            if (GetAsyncKeyState((int)hideKey) != 0)
            {
                programHidden = !programHidden;

                if (programHidden)
                {
                    ShowWindow(Process.GetCurrentProcess().MainWindowHandle, SW_HIDE);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("clicker hidden. press F3 to show.");
                }
                else
                {
                    ShowWindow(Process.GetCurrentProcess().MainWindowHandle, SW_SHOW);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("clicker shown. press F3 to hide.");
                }

                Console.ResetColor();
                Thread.Sleep(200);
                ShowMenu();
            }

            if (GetAsyncKeyState((int)processSelectKey) != 0)
            {
                specificProcessOnly = !specificProcessOnly;

                if (specificProcessOnly)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("specific process mode activated. clicks will only be performed in the selected process window.");
                    Console.WriteLine("press F4 again to select a different process or exit specific process mode.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("specific process mode deactivated. clicks will be performed in any active window.");
                }

                Console.ResetColor();
                Thread.Sleep(200);
                ShowMenu();
            }

            if (clicksPerSecond != previousClicksPerSecond)
            {
                previousClicksPerSecond = clicksPerSecond;
                Console.WriteLine("clicks per second adjusted to: " + clicksPerSecond);
                ShowMenu();
            }

            if (specificProcessOnly && targetProcess == null)
            {
                processes = GetActiveProcesses();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("select a process window for autoclicking:");

                for (int i = 0; i < processes.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {processes[i].MainWindowTitle}");
                }

                int processNumber;
                if (int.TryParse(Console.ReadLine(), out processNumber) && processNumber >= 1 && processNumber <= processes.Count)
                {
                    targetProcess = processes[processNumber - 1];
                    Console.WriteLine($"selected process: {targetProcess.ProcessName}");
                    ShowMenu();
                }
                else
                {
                    Console.WriteLine("invalid input. please select a process by its number.");
                    ShowMenu();
                }
            }

            Thread.Sleep(50);
        }
    }

    static void ShowMenu()
    {
        Console.WriteLine("--------------------");
        Console.WriteLine($"current CPS: {clicksPerSecond}");
        Console.WriteLine("--------------------");
        Console.WriteLine("press the following keys to make changes:");
        Console.WriteLine("F1 - toggle autoclicker");
        Console.WriteLine("F2 - adjust CPS");
        Console.WriteLine("F3 - hide/show clicker window");
        Console.WriteLine("F4 - toggle specific process mode");
        Console.WriteLine("--------------------");
    }

    static void AutoClick()
    {
        while (running)
        {
            if (autoclickerActive)
            {
                if (!specificProcessOnly || (specificProcessOnly && IsProcessActive(targetProcess)))
                {
                    LeftMouseClick();
                }
            }

            Thread.Sleep(1000 / clicksPerSecond);
        }
    }

    static void LeftMouseClick()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    static bool IsProcessActive(Process process)
    {
        IntPtr hwnd = process.MainWindowHandle;
        uint processId;
        GetWindowThreadProcessId(hwnd, out processId);

        return GetForegroundWindow() == hwnd && Process.GetCurrentProcess().Id == processId;
    }

    static List<Process> GetActiveProcesses()
    {
        Process[] processes = Process.GetProcesses();
        List<Process> activeProcesses = new List<Process>();

        foreach (Process process in processes)
        {
            if (process.MainWindowHandle != IntPtr.Zero && process.Id != Process.GetCurrentProcess().Id)
            {
                activeProcesses.Add(process);
            }
        }

        return activeProcesses;
    }

    static string GenerateRandomString(int length)
    {
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
