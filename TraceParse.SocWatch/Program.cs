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
        struct EnergyRecord
        {
            [Index(0)]
            public int SampleNumber { get; set; }

            [Index(1)]
            public float CTime { get; set; }

            [Index(2)]
            public float Duration { get; set; }

            [Index(3)]
            public float EnergyData { get; set; }
        }

        struct PowerRecord
        {
            [Index(0)]
            public int SampleNumber { get; set; }

            [Index(1)]
            public float CTime { get; set; }

            [Index(2)]
            public float Duration { get; set; }

            [Index(3)]
            public float PowerData { get; set; }
        }

        struct OutputRecord
        {
            [Index(0)]
            public float CTime { get; set; }

            [Index(1)]
            public float Value { get; set; }

            [Index(2)]
            public string Event { get; set; }

            [Index(3)]
            public string EventValue { get; set; }

            public OutputRecord(PowerRecord record, string eventName)
            {
                CTime = record.CTime;
                Value = record.PowerData;
                Event = eventName;
                if (!string.IsNullOrEmpty(eventName))
                    EventValue = Value.ToString();
                else
                    EventValue = string.Empty;
            }

            public OutputRecord(EnergyRecord record, string eventName)
            {
                CTime = record.CTime;
                Value = record.EnergyData;
                Event = eventName;
                if (!string.IsNullOrEmpty(eventName))
                    EventValue = Value.ToString();
                else
                    EventValue = string.Empty;
            }
        }

        struct EventRecord
        {
            [Index(0)]
            public string Event { get; set; }

            [Index(1)]
            public float CTime { get; set; }
        }

        static void Main(string[] args)
        {

            var file = "trace.csv";
            if (args.Length > 0)
            {
                file = args[0];
            }

            var powerRecordFile = "power.csv";
            var energyRecordFile = "energy.csv";
            var energyRecords = new List<EnergyRecord>();
            var powerRecords = new List<PowerRecord>();
            var eventRecords = new List<EventRecord>();
            var programStartTime = string.Empty;
            var dataCollectionStartTime = string.Empty;

            using (var reader = new StreamReader(file))
            {
                using (var csv = new CsvReader(reader))
                {
                    while (csv.Read())
                    {
                        if (csv.Context.Record[0].StartsWith("Program Started"))
                        {
                            programStartTime = csv.Context.Record[0];
                        }

                        if (csv.Context.Record[0].StartsWith("Data Collection Started"))
                        {
                            dataCollectionStartTime = csv.Context.Record[0];
                        }

                        if (csv.Context.Record[0].StartsWith("Package Power - Package_0"))
                        {
                            csv.Read();
                            csv.ReadHeader();
                            break;
                        }
                    }


                    while (csv.Read())
                    {
                        if (csv.Context.Record[0].StartsWith("Package Power - Package_0 : Instantaneous rate"))
                        {
                            csv.Read();
                            csv.ReadHeader();
                            break;
                        }
                        var record = csv.GetRecord<EnergyRecord>();
                        record.CTime /= 1000000;
                        energyRecords.Add(record);
                    }

                    while (csv.Read())
                    {
                        var record = csv.GetRecord<PowerRecord>();
                        record.CTime /= 1000000;
                        powerRecords.Add(record);
                    }
                }
            }

            using (var reader = new StreamReader("log.csv"))
            {
                using (var csv = new CsvReader(reader))
                {
                    csv.Read();
                    csv.ReadHeader();
                    csv.Read();
                    while (csv.Read())
                    {
                        var record = csv.GetRecord<EventRecord>();
                        eventRecords.Add(record);
                    }
                }
            }

            int eventIndex = 0;
            var outputList = new List<OutputRecord>();
            using (var writer = new StreamWriter(powerRecordFile))
            {
                using (var csv = new CsvWriter(writer))
                {
                    foreach (var record in powerRecords)
                    {
                        var eventName = string.Empty;
                        if (eventIndex < eventRecords.Count && eventRecords[eventIndex].CTime < record.CTime)
                        {
                            eventName = eventRecords[eventIndex].Event;
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
                    foreach (var record in energyRecords)
                    {
                        var eventName = string.Empty;
                        if (eventIndex < eventRecords.Count && eventRecords[eventIndex].CTime < record.CTime)
                        {
                            eventName = eventRecords[eventIndex].Event;
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
