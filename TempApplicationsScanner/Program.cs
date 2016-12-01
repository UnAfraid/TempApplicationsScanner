using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WmiLight;

namespace TempApplicationScanner
{
    class Program
    {
        HashSet<string> scannedProcesses = new HashSet<string>();

        public Program()
        {
            // Create a timer
            var myTimer = new System.Timers.Timer();
            myTimer.Elapsed += new ElapsedEventHandler(scanProcessList);
            myTimer.Interval = 3000;
            myTimer.Enabled = true;

            Console.WriteLine("Scanning for the applications that run from your temp folder");
            Console.WriteLine("Write 'stop' to stop scanning");
            while (true)
            {
                string line = Console.ReadLine();
                if (line != null && line.Equals("stop"))
                {
                    break;
                }
                Thread.Sleep(500);
            }
        }

        private void scanProcessList(object source, ElapsedEventArgs args)
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                try
                {
                    string fullyQualifiedPath = ProcessExtension.GetProcessName(process);
                    string executableDirectory = Path.GetDirectoryName(fullyQualifiedPath);
                    if (executableDirectory != null && executableDirectory.StartsWith(Path.GetTempPath()) && scannedProcesses.Add(process.ToString()))
                    {
                        // ProcessExtension.Suspend(process);
                        Console.WriteLine("Process: PID: {0} Name: {1} params: {2}", process.Id, fullyQualifiedPath, GetCommandLine(process));
                        Console.WriteLine("Working directory: {0}", fullyQualifiedPath);
                        printParentProcessses(process);
                        //CopyFilesRecursively(new DirectoryInfo(Path.GetDirectoryName(process.MainModule.FileName)), new DirectoryInfo(@"D:\Inspection"));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Problem with accessing process: {0}, {1}", process, e);
                }
            }
        }


        private string GetCommandLine(Process process)
        {
            string cmdLine = null;
            using (WmiConnection con = new WmiConnection())
            {
                foreach (WmiObject obj in con.CreateQuery($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    cmdLine = obj["CommandLine"]?.ToString().Trim();
                }
            }
            return cmdLine;
        }

        /*
        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                string destination = dir.Name;
                if (!Directory.Exists(destination))
                {
                    CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                }
            }
            foreach (FileInfo file in source.GetFiles())
            {
                string destination = Path.Combine(target.FullName, file.Name);
                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }
                file.CopyTo(Path.Combine(target.FullName, file.Name));
            }
        }
        */

        private static void printParentProcessses(Process process)
        {
            Process parent = process;
            try
            {
                for (int times = 0; times < 100; times++)
                {
                    parent = ParentProcessUtilities.GetParentProcess(parent.Id);
                    if (parent == null)
                    {
                        break;
                    }

                    printDashes(times);
                    Console.WriteLine("> Parent process: {0} ", parent);
                    printDashes(times);
                    Console.WriteLine("> Working directory: {0}", ProcessExtension.GetProcessName(parent));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to inspect parent process: {0} {1}", parent, e);
            }
        }

        private static void printDashes(int times)
        {
            for (int dashes = 0; dashes < times; dashes++)
            {
                Console.Write("-");
            }
        }

        static void Main(string[] args)
        {
            new Program();
        }
    }
}
