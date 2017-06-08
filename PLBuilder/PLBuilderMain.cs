using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace PLBuilder
{
    class Options
    {
        [Option('d', "dictionaryFile", Required = false,
          HelpText = "Provide your own Dictionary File.")]
        public string DictionaryFile { get; set; }

        [Option('o', "outputFolder", Required = false,
            HelpText = "Specify an output folder.")]
        public string OutputFolder { get; set; }

        [Option('s', "projectFile", Required = false,
          HelpText = "Provide your own Solution File.")]
        public string ProjectFile { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var usage = new StringBuilder();
            usage.AppendLine("Quickstart Application 1.0");
            usage.AppendLine("Read user manual for usage instructions...");
            return usage.ToString();
        }

    }

    class PLBuilderMain
    {
        static private XmlTextReader _programConf;
        static private XmlTextReader _userConf;
        static private string _outputFolder;
        static private List<String> _dictionary;
        static private List<String> _usedWords;
        static private Random _randGen; 

        static void Main(string[] args)
        {
            string dictionaryFile = "";
            string projectFile = "";

            _randGen = new Random();

            _programConf = new XmlTextReader("ProgramConf.xml");
            _userConf = new XmlTextReader("UserConf.xml");

            _programConf.ReadToFollowing("DictionaryFile");
            _programConf.Read();
            dictionaryFile = _programConf.Value;

            _programConf.ReadToFollowing("OutputFolder");
            _programConf.Read();
            _outputFolder = _programConf.Value;

            _programConf.ReadToFollowing("ProjectFile");
            _programConf.Read();
            projectFile = _programConf.Value;

            //Overwrite defaults and get other runtime options
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                if(options.DictionaryFile != null)
                {
                    dictionaryFile = options.DictionaryFile;
                }

                if (options.OutputFolder != null)
                {
                    _outputFolder = options.OutputFolder;
                }

                if (options.ProjectFile != null)
                {
                    projectFile = options.ProjectFile;
                }
            }

            Console.WriteLine(projectFile);

            var fileNames = GetFileNames(projectFile, !projectFile.Contains(":\\"));

            foreach(var name in fileNames)
            {
                Console.WriteLine(name);
            }

            var remScripts = getRemoteScriptList();

            foreach(var s in remScripts)
            {
                Console.WriteLine(s);
            }

            var localScripts = getRemoteScriptList();

            buildDictionary(dictionaryFile);

            writeFunctionFile(remScripts, localScripts);

        }

        static void buildDictionary(string dictionaryFile)
        {
            _dictionary = new List<string>();
            _usedWords = new List<string>();

            foreach(var currWord in File.ReadAllLines(dictionaryFile))
            {
                _dictionary.Add(currWord);
            }
        }

        static List<string> GetFileNames(string projectFile, bool relative)
        {
            List<string> fileNames = new List<string>();

            if (relative)
            {
                string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                projectFile = Path.GetFullPath(Path.Combine(folder, @projectFile));
            }

            Console.WriteLine(projectFile);

            var fin = File.ReadAllLines(projectFile);
            int lastIdx = projectFile.LastIndexOf('\\');
            projectFile = projectFile.Substring(0, lastIdx + 1);

            foreach (var line in fin)
            {
                if(line.Contains(".cs"))
                {
                    fileNames.Add( projectFile + line.Split('"')[1]);
                }
            }

            DirectoryCopy(projectFile, _outputFolder, true);

            return fileNames;
        }

        //https://msdn.microsoft.com/en-us/library/bb762914(v=vs.110).aspx
        static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath,true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        static List<string> getRemoteScriptList()
        {
            List<string> scripts = new List<string>();

            while (_userConf.ReadToFollowing("Remote"))
            {
                _userConf.Read();
                scripts.Add(_userConf.Value);
            }

            return scripts;
        }

        static List<string> getLocalScriptList()
        {
            List<string> scripts = new List<string>();

            while (_userConf.ReadToFollowing("Local"))
            {
                _userConf.Read();
                scripts.Add(_userConf.Value);
            }

            return scripts;
        }

        static void writeFunctionFile(List<string> remScripts, List<string> localScripts)
        {
            //make this better at some point
            var fin = System.IO.File.ReadAllLines(_outputFolder + "Functions.cs");

            StreamWriter fout = new StreamWriter(_outputFolder + "Functions.cs");

            int idx = 0;

            foreach(var line in fin)
            {
                idx++;
                if(line.Contains("$$"))
                {
                    break;
                }

                fout.WriteLine(line);
            }

            //write the XOR key
            byte dKey = Convert.ToByte( GetLetter() );
            fout.WriteLine("\t\t\tdKey = \'" + (char) dKey + "\';");

            foreach(var script in remScripts)
            {
                var scriptName = script.Substring(script.LastIndexOf('/') + 1);
                using (var client = new WebClient())
                {
                    client.DownloadFile(new Uri(script), _outputFolder +  scriptName);
                    Byte[] bytes = File.ReadAllBytes(_outputFolder + scriptName);

                    //XOR "encrypt"
                    for(int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] ^= dKey;
                    }

                    string b64XorScript = Convert.ToBase64String(bytes);

                    //Lop off PS1 or other file extension from scriptname
                    Byte[] moduleName = Encoding.UTF8.GetBytes(scriptName.Split('.')[0]);
                    Byte[] outModuleName = new Byte[moduleName.Length];

                    for (int i =0; i < moduleName.Length; i++)
                    {
                        outModuleName[i] = (byte)(moduleName[i] ^ dKey);
                    }

                    fout.WriteLine("\t\t\tFuncs.Add(\"" + Convert.ToBase64String(outModuleName) + "\",\"" + b64XorScript + "\");");
                }
            }

            for(int i = idx; i < fin.Length; i++)
            {
                fout.WriteLine(fin[i]);
            }

            fout.Close();
            //string cmdStr = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\MSBuild.exe /b ";
            string currPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string fullPath = Path.Combine(currPath, _outputFolder + "PowerLine.sln");
            //cmdStr += " /t:rebuild /p:PlatformTarget = x64";
            //Console.WriteLine(cmdStr);

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
           // cmd.StartInfo.RedirectStandardOutput = true;
           // cmd.StartInfo.RedirectStandardError = true;
            //cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe " + fullPath + @" /t:rebuild /p:PlatformTarget=x64");
            //cmd.StandardInput.WriteLine(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe C:\Users\fmc\Source\Repos\powerline\PLWTF\PowerLine.sln /t:rebuild /p:PlatformTarget=x64");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
          //  Console.WriteLine(cmd.StandardOutput.ReadToEnd());
          //  Console.WriteLine(cmd.StandardError.ReadToEnd());

        }

        //https://stackoverflow.com/questions/15249138/pick-random-char
        public static char GetLetter()
        {
            string chars = "$%#@!*abcdefghijklmnopqrstuvwxyz1234567890?;:ABCDEFGHIJKLMNOPQRSTUVWXYZ^&";
            int num = _randGen.Next(0, chars.Length - 1);
            return chars[num];
        }

        //Gets a random word from the dictionary and makes sure it isnt already in use
        public static string GetWord()
        {
            while (true)
            {
                int num = _randGen.Next(0, _dictionary.Count - 1);
                if ( !_usedWords.Contains( _dictionary[num] ) )
                {
                    _usedWords.Add(_dictionary[num]);
                    return _dictionary[num];
                }
            }
        }
    }
}
