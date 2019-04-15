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

        static void Main(string[] args)
        {
            var file = "runs-dod/sp1_trace.csv";
            var logFile = "runs-dod/sp1_log.csv";
            if (args.Length > 0)
            {
                file = args[0];
            }
            if (args.Length > 1)
            {
                logFile = args[1];
            }

            var parseOutput = Parser.ParseFile(file, logFile);

            var powerRecordFile = "power.csv";
            var energyRecordFile = "energy.csv";
            var frameRecordFile = "frame.csv";

            using (var writer = new StreamWriter(frameRecordFile))
            {
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(parseOutput.FrameRecords);
                }
            }

            int eventIndex = 0;
            var outputList = new List<OutputRecord>();
            using (var writer = new StreamWriter(powerRecordFile))
            {
                using (var csv = new CsvWriter(writer))
                {
                    foreach (var record in parseOutput.PowerRecords)
                    {
                        var eventName = string.Empty;
                        while (eventIndex < parseOutput.EventRecords.Count && parseOutput.EventRecords[eventIndex].CTime < record.CTime)
                        {
                            eventName = parseOutput.EventRecords[eventIndex].Event;
                            eventIndex++;
                        }
                        var outputRecord = new OutputRecord(record, eventName);
                        outputList.Add(outputRecord);

                    }
                    csv.WriteRecords(outputList);
                }
            }

            eventIndex = 0;
            outputList.Clear();
            using (var writer = new StreamWriter(energyRecordFile))
            {
                using (var csv = new CsvWriter(writer))
                {
                    foreach (var record in parseOutput.EnergyRecords)
                    {
                        var eventName = string.Empty;
                        while (eventIndex < parseOutput.EventRecords.Count && parseOutput.EventRecords[eventIndex].CTime < record.CTime)
                        {
                            eventName = parseOutput.EventRecords[eventIndex].Event;
                            eventIndex++;
                        }
                        var outputRecord = new OutputRecord(record, eventName);
                        outputList.Add(outputRecord);
                    }
                    csv.WriteRecords(outputList);
                }
            }
        }
    }
}
