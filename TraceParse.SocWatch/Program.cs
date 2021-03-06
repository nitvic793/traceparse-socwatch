﻿using CommandLine;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceParse.SocWatch
{
    class Program
    {
        class Options
        {
            [Option('p', "parse", Required = true, HelpText = "Parse given trace file and log file.", SetName = "Single")]
            public bool Parse { get; set; }

            [Option('a', "average", Required = true, HelpText = "Parse a batch of files and get average of results.", SetName = "Multi")]
            public bool Average { get; set; }

            [Option('t', "tracefile", Required = true, HelpText = "Input trace file to be processed.", SetName = "Single")]
            public string InputTraceFile { get; set; }

            [Option('l', "logfile", Required = true, HelpText = "Input log file to be processed.", SetName = "Single")]
            public string InputLogFile { get; set; }

            [Option('f', "folder", Required = true, HelpText = "Folder with files to be processed.", SetName = "Multi")]
            public string RelativeFolder { get; set; }
        }

        static void ParseSingleTraceFile(string traceFile, string logFile)
        {
            var parseOutput = TraceParser.ParseFile(traceFile, logFile);
            TraceParser.WriteRecords(parseOutput);
        }

        static void ParseBatch(string folder)
        {
            var files = Directory.GetFiles(folder, "*.csv");
            var traceFiles = files.Where(s => s.Contains("_trace.csv")).ToArray();
            var logFiles = files.Where(s => s.Contains("_log.csv")).ToArray();
            var outputs = new List<DataOutput>();
            var index = 0;
            foreach (var csv in traceFiles)
            {
                var parseOutput = TraceParser.ParseFile(csv, logFiles[index]);
                outputs.Add(parseOutput);
                index++;
            }

            var output = TraceParser.GetAverage(outputs);
            TraceParser.WriteRecords(output);
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(opts =>
                    {
                        if (opts.Parse)
                        {
                            ParseSingleTraceFile(opts.InputTraceFile, opts.InputLogFile);
                        }
                        else if(opts.Average)
                        {
                            ParseBatch(opts.RelativeFolder);
                        }
                    })
                    .WithNotParsed((errs) =>
                    {
                        foreach (var err in errs)
                        {
                            Console.WriteLine(err);
                        }
                    });
        }
    }
}
