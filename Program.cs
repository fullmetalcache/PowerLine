using System;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text;
using System.Runtime.InteropServices;

namespace PowerLine
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("\nPlease provide at least Script Name...\n");
                Console.WriteLine("Typical usage: PowerLine.exe ScriptName \"Method MethodArguments\"\n");
                Console.WriteLine("To see which scripts are available, run as: PowerLine.exe -ShowScripts\n");
                Console.WriteLine();
                return;
            }

            if (args.Length > 2 && ( args[2].ToLower() == "-b" ))
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
            }

            Functions.InitDictionary();

            args[0] = args[0].ToLower();

            if (args[0] == "-showscripts")
            {
                MyCode.ShowScripts();
                return;
            }
            else
            {
                if (!Functions.Funcs.ContainsKey(args[0]))
                {
                    Console.WriteLine("Script: " + args[0] + " is not currently present in the program");
                    return;
                }

                MyCode.ExecuteFunc(args);
            }
        }
    }

    public class MyCode
    {
        public static void ExecuteFunc(string[] args)
        {
            string script = args[0];
            string command = Encoding.UTF8.GetString(Convert.FromBase64String(Functions.Funcs[script]));

            if (args.Length > 1)
            {
                Console.WriteLine(args[1]);
                string parameters = "\n" + args[1] + "\n";
                command += parameters;
            }

            Runspace rspace = RunspaceFactory.CreateRunspace();
            rspace.Open();
            Pipeline pipeline = rspace.CreatePipeline();
            pipeline.Commands.AddScript(command);
            Collection<PSObject> results = pipeline.Invoke();
        }

        public static void ShowScripts()
        {
            foreach (KeyValuePair<string, string> kvp in Functions.Funcs)
            {
                Console.WriteLine("");
                Console.WriteLine(kvp.Key);
            }
        }
    }
}
