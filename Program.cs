using System;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

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
            if (args.Length > 2 && (args[2].ToLower() == "-b"))
            {
                IntPtr handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
            }

            MyCode.StartHere(args);
        }
    }

    [System.ComponentModel.RunInstaller(true)]
    public class Sample : System.Configuration.Install.Installer
    {
        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            System.IO.StreamReader file = new System.IO.StreamReader("prog_args.txt");
            string line;
            string [] args = new string[15];
            int counter = 0;
            while((line = file.ReadLine()) != null)
            {
                args[counter] = line;
                counter++;
            }

            MyCode.StartHere(args);
        }
    }

    public class MyCode
    {
        public static void StartHere(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("\nPlease provide at least a Script Name...\n");
                Console.WriteLine("Typical usage: PowerLine.exe ScriptName \"Method MethodArguments\"\n");
                Console.WriteLine("To see which scripts are available, run as: PowerLine.exe -ShowScripts\n");
                Console.WriteLine();
                return;
            }

            Functions.InitDictionary();

            args[0] = args[0].ToLower();

            if (args[0] == "-showscripts")
            {
                ShowScripts();
                return;
            }
            else
            {
                if (!Functions.Funcs.ContainsKey(args[0]))
                {
                    Console.WriteLine("Script: " + args[0] + " is not currently present in the program");
                    return;
                }

                ExecuteFunc(args);
            }
        }

        public static void ExecuteFunc(string[] args)
        {
            string script = args[0];
            string command = Encoding.UTF8.GetString(Convert.FromBase64String(Functions.Funcs[script]));

            if (args.Length > 1)
            {
                Console.WriteLine("\nCommand Invoked: " + args[1]);
                string parameters = "\n" + args[1] + "\n";
                command += parameters;
            }

            //Runs powershell stuff
            MyPSHost myPSHost = new MyPSHost();
            Runspace rspace = RunspaceFactory.CreateRunspace(myPSHost);
            rspace.Open();
            Pipeline pipeline = rspace.CreatePipeline();
            pipeline.Commands.AddScript(command);
            pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            pipeline.Commands.Add("out-default");
            pipeline.InvokeAsync();

            int idx = 0;
            List<string> currList;
            while (pipeline.PipelineStateInfo.State == PipelineState.Running)
            {
                currList = ((MyPSHostUI)myPSHost.UI).Output;
                while(idx < currList.Count)
                {
                    Console.Write(currList[idx]);
                    idx++;
                }

                Thread.Sleep(100);
            }

            currList = ((MyPSHostUI)myPSHost.UI).Output;
            while (idx < currList.Count)
            {
                Console.Write(currList[idx]);
                idx++;
            }

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
