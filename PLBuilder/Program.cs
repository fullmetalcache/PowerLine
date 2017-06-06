using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace PLBuilder
{
    class Program
    {
        static private XmlTextReader _programConf;
        static private XmlTextReader _userConf;

        //For the command line parser
        class Options
        {
            [Option('d', "dictionaryFile", Required = false,
              HelpText = "Provide your own Dictionary File for Obfuscation.")]
            public string DictionaryFile { get; set; }

            [Option('s', "solutionFile", Required = false,
              HelpText = "Provide your own Solution File.")]
            public string SolutionFile { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }

        static void Main(string[] args)
        {
            string dictionaryFile = "";
            string solutionFile = "";

            _programConf = new XmlTextReader("ProgramConf.xml");
            _userConf = new XmlTextReader("UserConf.xml");

            _programConf.ReadToDescendant("DictionaryFile");
            _programConf.Read();
            dictionaryFile = _programConf.Value;

            _programConf.ReadToNextSibling("SolutionFile");
            _programConf.Read();
            solutionFile = _programConf.Value;

            //Overwrite defaults and get other runtime options
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                if (options.DictionaryFile.Length > 1)
                {
                    dictionaryFile = options.DictionaryFile;
                }
                if (options.SolutionFile.Length > 1)
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
