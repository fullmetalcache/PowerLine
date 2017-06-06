using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace PLBuilder
{
    class Options
    {
        [Option('d', "dictionaryFile", Required = false,
          HelpText = "Provide your own Dictionary File.")]
        public string DictionaryFile { get; set; }

        [Option('s', "solutionFile", Required = false,
          HelpText = "Provide your own Solution File.")]
        public string SolutionFile { get; set; }

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

        static void Main(string[] args)
        {
            string dictionaryFile = "";
            string solutionFile = "";

            _programConf = new XmlTextReader("ProgramConf.xml");
            _userConf = new XmlTextReader("UserConf.xml");

            _programConf.ReadToFollowing("DictionaryFile");
            _programConf.Read();
            dictionaryFile = _programConf.Value;

            _programConf.ReadToFollowing("SolutionFile");
            _programConf.Read();
            solutionFile = _programConf.Value;

            //Overwrite defaults and get other runtime options
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                if(options.DictionaryFile != null)
                {
                    dictionaryFile = options.DictionaryFile;
                }

                if (options.SolutionFile != null)
                {
                    solutionFile = options.SolutionFile;
                }
            }
        }

       // static string[] GetFileNames(string solutionFile)
       // {
            //string[] fileNames = new string();
       // }
    }
}
