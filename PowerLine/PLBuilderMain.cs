
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace PLBuilder
{
    //class Options
    //{
    //    [Option('d', "dictionaryFile", Required = false,
    //      HelpText = "Provide your own Dictionary File.")]
    //    public string DictionaryFile { get; set; }

    //    [Option('s', "projectFile", Required = false,
    //      HelpText = "Provide your own Solution File.")]
    //    public string ProjectFile { get; set; }

    //    [HelpOption]
    //    public string GetUsage()
    //    {
    //        var usage = new StringBuilder();
    //        usage.AppendLine("Quickstart Application 1.0");
    //        usage.AppendLine("Read user manual for usage instructions...");
    //        return usage.ToString();
    //    }

    //}

    class PLBuilderMain
    {


        static private XmlTextReader _programConf;
        static private XmlTextReader _userConf;
        static private string _saveDir;
        static private List<String> _savedFiles;
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

            _programConf.ReadToFollowing("ProjectFile");
            _programConf.Read();
            projectFile = _programConf.Value;

            //Overwrite defaults and get other runtime options
            //var options = new Options();
            //if (Parser.Default.ParseArguments(args, options))
            //{
            //    if(options.DictionaryFile != null)
            //    {
            //        dictionaryFile = options.DictionaryFile;
            //    }

            //    if (options.ProjectFile != null)
            //    {
            //        projectFile = options.ProjectFile;
            //    }
            //}

            PrintNorm("Getting Template Source Files From:" + projectFile);

            PrintNorm("Saving Template Files");
            List<string> fileNames = GetFileNames(projectFile, !projectFile.Contains(":\\"));

            string functionsFile = "";
            foreach (string name in fileNames)
            {
                if (name.Contains("Functions.cs"))
                {
                    functionsFile = name;
                    break;
                }
            }

            List<string> remScripts = getRemoteScriptList();
            List<string> localScripts = getRemoteScriptList();

            PrintNorm("Building Obfuscation Dictionary From: " + dictionaryFile);
            buildDictionary(dictionaryFile);

            bool success = writeFunctionFile(functionsFile, remScripts, localScripts);

            PrintNorm("Restoring Template Files and Deleting Temp Files");
            restoreFiles();

            if (success)
            {
                PrintNorm("Sucess! Run the PowerLine.exe Program");
            }
            else
            {
                PrintError("Build Errors Occured, Please Check the MSBuild Output");
            }

        }

        static void buildDictionary(string dictionaryFile)
        {
            _dictionary = new List<string>();
            _usedWords = new List<string>();

            foreach (string currWord in File.ReadAllLines(dictionaryFile))
            {
                _dictionary.Add(currWord);
            }
        }

        static List<string> GetFileNames(string projectFile, bool relative)
        {
            List<string> fileNames = new List<string>();
            _savedFiles = new List<string>();

            if (relative)
            {
                string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                projectFile = Path.GetFullPath(Path.Combine(folder, @projectFile));
            }

            string[] fin = File.ReadAllLines(projectFile);

            System.String projectFolder = Path.GetDirectoryName(projectFile);
            _saveDir = Path.Combine(projectFolder, "plsave");

            foreach (string line in fin)
            {
                if (line.Contains(".cs"))
                {
                    string fileNameCurr = line.Split('"')[1];
                    string fileNamePath = Path.Combine(projectFolder, fileNameCurr);
                    fileNames.Add(fileNamePath);

                    string savePath = Path.Combine(_saveDir, fileNameCurr);
                    string savePathFolder = Path.GetDirectoryName(savePath);
                    if (!Directory.Exists(savePathFolder))
                    {
                        Directory.CreateDirectory(savePathFolder);
                    }

                    _savedFiles.Add(fileNamePath + "$$$" + savePath);

                    File.Copy(fileNamePath, savePath, true);
                }
            }

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
                file.CopyTo(temppath, true);
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

        static bool writeFunctionFile(string functionsFile, List<string> remScripts, List<string> localScripts)
        {
            //make this better at some point

            string[] fin = File.ReadAllLines(functionsFile);

            StreamWriter fout = new StreamWriter(functionsFile, false);

            int idx = 0;

            foreach (string line in fin)
            {
                idx++;
                if (line.Contains("$$$"))
                {
                    break;
                }

                fout.WriteLine(line);
            }

            //write the XOR key
            byte dKey = Convert.ToByte(GetLetter());
            fout.WriteLine("\t\t\tdKey = \'" + (char)dKey + "\';");

            PrintNorm("Importing and Encoding Scripts");

            foreach (string script in remScripts)
            {
                string scriptName = script.Substring(script.LastIndexOf('/') + 1);
                using (WebClient client = new WebClient())
                {
                    client.Proxy = WebRequest.GetSystemWebProxy();
                    client.Proxy.Credentials = CredentialCache.DefaultCredentials;
                    using (MemoryStream mStream = new MemoryStream(client.DownloadData(new Uri(script))))
                    {

                        Console.WriteLine("\t" + script + "\r\n");
                        byte[] bytes = mStream.ToArray();

                        //XOR "encrypt"
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bytes[i] ^= dKey;
                        }

                        string b64XorScript = Convert.ToBase64String(bytes);

                        //Lop off PS1 or other file extension from scriptname
                        Byte[] moduleName = Encoding.UTF8.GetBytes(scriptName.Split('.')[0]);
                        Byte[] outModuleName = new Byte[moduleName.Length];

                        for (int i = 0; i < moduleName.Length; i++)
                        {
                            outModuleName[i] = (byte)(moduleName[i] ^ dKey);
                        }

                        fout.WriteLine("\t\t\tFuncs.Add(\"" + Convert.ToBase64String(outModuleName) + "\",\"" + b64XorScript + "\");");
                    }
                }
            }

            for (int i = idx; i < fin.Length; i++)
            {
                fout.WriteLine(fin[i]);
            }

            fout.Close();

            PrintNorm("Building PowerLine.exe");

            string currPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string fullPath = "";

            string buildTool = "";

            if(File.Exists(@"C:\Windows\Microsoft.NET\Framework64\v2.0.50727\MSBuild.exe"))
            {
                buildTool = @"C:\Windows\Microsoft.NET\Framework64\v2.0.50727\MSBuild.exe";
                fullPath = Path.Combine(Path.GetDirectoryName(functionsFile), "PowerLineTemplateWin7.pln.sln");
            } else if(File.Exists(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"))
            {
                buildTool = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe";
                fullPath = Path.Combine(Path.GetDirectoryName(functionsFile), "PowerLineTemplateWin10.pln.sln");
            }

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.Arguments = "/c " + buildTool + " " + fullPath + @" /t:rebuild /p:Configuration=Release /p:Platform=x64";
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.WaitForExit();

            return (cmd.ExitCode == 0);
        }

        public static void restoreFiles()
        {
            string[] splitVal = new string[] { "$$$" };

            foreach (string name in _savedFiles)
            {
                string destPath = name.Split(splitVal, StringSplitOptions.RemoveEmptyEntries)[0];
                string srcPath = name.Split(splitVal, StringSplitOptions.RemoveEmptyEntries)[1];
                File.Copy(srcPath, destPath, true);
            }

            Directory.Delete(_saveDir, true);
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
                if (!_usedWords.Contains(_dictionary[num]))
                {
                    _usedWords.Add(_dictionary[num]);
                    return _dictionary[num];
                }
            }
        }

        public static void PrintNorm(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("\r\n###### " + msg + " ######\r\n");

            Console.ResetColor();
        }

        public static void PrintError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("\r\n!!!!! " + msg + " !!!!!\r\n");

            Console.ResetColor();
        }
    }
}
