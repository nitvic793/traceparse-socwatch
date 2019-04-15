﻿using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceParse.SocWatch
{
    public struct EnergyRecord
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

    public struct PowerRecord
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

    public struct OutputRecord
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

    public struct EventRecord
    {
        [Index(0)]
        public string Event { get; set; }

        [Index(1)]
        public float CTime { get; set; }
    }

    public struct FrameRecord
    {
        [Index(0)]
        public int FrameNumber { get; set; }

        [Index(1)]
        public float CTime { get; set; }

        [Index(2)]
        public float Power { get; set; }

        [Index(3)]
        public float Energy { get; set; }

        /// <summary>
        /// These averages are the average of the samples between the previous and the current frame.
        /// </summary>
        [Index(4)]
        public float PowerAverage { get; set; }

        /// <summary>
        /// These averages are the average of the samples between the previous and the current frame.
        /// </summary>
        [Index(5)]
        public float EnergyAverage { get; set; }

        [Index(6)]
        public float SampleCount { get; set; }
    }

    public struct DataOutput
    {
        public string ProgramStartTime;
        public string DataCollectionTime;
        public List<PowerRecord> PowerRecords;
        public List<EnergyRecord> EnergyRecords;
        public List<EventRecord> EventRecords;
        public List<FrameRecord> FrameRecords;
    }
    public static class Parser
    {
        public static DataOutput ParseFile(string file, string logFile)
        {
            DataOutput output = new DataOutput();
            output.EnergyRecords = new List<EnergyRecord>();
            output.PowerRecords = new List<PowerRecord>();
            output.EventRecords = new List<EventRecord>();

            using (var reader = new StreamReader(file))
            {
                using (var csv = new CsvReader(reader))
                {
                    //Read and discard header details
                    while (csv.Read())
                    {
                        if (csv.Context.Record[0].StartsWith("Program Started"))
                        {
                            output.ProgramStartTime = csv.Context.Record[0];
                        }

                        if (csv.Context.Record[0].StartsWith("Data Collection Started"))
                        {
                            output.DataCollectionTime = csv.Context.Record[0];
                        }

                        if (csv.Context.Record[0].StartsWith("Package Power - Package_0"))
                        {
                            csv.Read();
                            csv.ReadHeader();
                            break;
                        }
                    }

                    //Read energy records
                    while (csv.Read())
                    {
                        if (csv.Context.Record[0].StartsWith("Package Power - Package_0 : Instantaneous rate"))
                        {
                            csv.Read();
                            csv.ReadHeader();
                            break; //break if CSV file's power section is reads
                        }
                        var record = csv.GetRecord<EnergyRecord>();
                        record.CTime /= 1000000; // convert nanoseconds to seconds
                        output.EnergyRecords.Add(record);
                    }

                    //Read power records
                    while (csv.Read())
                    {
                        var record = csv.GetRecord<PowerRecord>();
                        record.CTime /= 1000000; // convert nanoseconds to seconds
                        output.PowerRecords.Add(record);
                    }
                }
            }

            //Read log file into event list
            using (var reader = new StreamReader(logFile))
            {
                using (var csv = new CsvReader(reader))
                {
                    csv.Read();
                    csv.ReadHeader();
                    csv.Read();
                    while (csv.Read())
                    {
                        var record = csv.GetRecord<EventRecord>();
                        output.EventRecords.Add(record);
                    }
                }
            }

            //Read frame numbers from log data
            output.FrameRecords = new List<FrameRecord>();
            for (int i = 2; i < output.EventRecords.Count; ++i) //skip first two records
            {
                var frame = Convert.ToInt32(output.EventRecords[i].Event);
                var time = output.EventRecords[i].CTime;
                output.FrameRecords.Add(new FrameRecord
                {
                    FrameNumber = frame,
                    CTime = time,
                    Energy = 0F,
                    Power = 0F
                });
            }

            var frameIndex = 0;
            float pValue = 0F;
            float eValue = 0F;

            int count = 0;
            for (int i = 0; i < output.PowerRecords.Count;)
            {
                if (frameIndex >= output.FrameRecords.Count)
                {
                    break;
                }

                if (output.FrameRecords[frameIndex].CTime > output.PowerRecords[i].CTime)
                {
                    pValue += output.PowerRecords[i].PowerData;
                    eValue += output.EnergyRecords[i].EnergyData;
                    count++;
                    i++;
                }
                else
                {
                    
                }

                if(output.FrameRecords[frameIndex].CTime <= output.PowerRecords[i].CTime)
                {
                    var frameRecord = output.FrameRecords[frameIndex];
                    if (count != 0)
                    {
                        frameRecord.Power = pValue;
                        frameRecord.Energy = eValue;
                        frameRecord.PowerAverage = pValue / count; 
                        frameRecord.EnergyAverage = eValue / count;
                        frameRecord.SampleCount = count;
                    }
                    else
                    {
                        frameRecord.Power = output.PowerRecords[i].PowerData;
                        frameRecord.Energy = output.EnergyRecords[i].EnergyData;
                        frameRecord.PowerAverage = output.PowerRecords[i].PowerData;
                        frameRecord.EnergyAverage = output.EnergyRecords[i].EnergyData;
                        frameRecord.SampleCount = 1;
                    }
                    output.FrameRecords[frameIndex] = frameRecord;
                    pValue = eValue = 0F;
                    count = 0;
                    frameIndex++;
                }

            }

            return output;
        }

        public static DataOutput GetAverage(List<DataOutput> outputs)
        {
            DataOutput output = new DataOutput();

            output.EnergyRecords = new List<EnergyRecord>(outputs[0].EnergyRecords.Count);
            output.PowerRecords = new List<PowerRecord>(outputs[0].PowerRecords.Count);
            int sampleSize = outputs[0].PowerRecords.Count;
            for (int i = 0; i < sampleSize; ++i)
            {
                var energyRecord = new EnergyRecord()
                {
                    SampleNumber = 0,
                    EnergyData = 0F,
                    CTime = 0F,
                    Duration = 0F
                };

                var powerRecord = new PowerRecord()
                {
                    SampleNumber = 0,
                    PowerData = 0F,
                    CTime = 0F,
                    Duration = 0F
                };

                for (int k = 0; k < outputs.Count; ++i)
                {
                    energyRecord.SampleNumber = outputs[k].EnergyRecords[i].SampleNumber;
                    energyRecord.CTime += outputs[k].EnergyRecords[i].CTime;
                    energyRecord.Duration += outputs[k].EnergyRecords[i].Duration;
                    energyRecord.EnergyData += outputs[k].EnergyRecords[i].EnergyData;

                    powerRecord.SampleNumber = outputs[k].PowerRecords[i].SampleNumber;
                    powerRecord.CTime += outputs[k].PowerRecords[i].CTime;
                    powerRecord.Duration += outputs[k].PowerRecords[i].Duration;
                    powerRecord.PowerData += outputs[k].PowerRecords[i].PowerData;
                }

                //Get Average across one row and store it
                energyRecord.CTime /= outputs.Count;
                energyRecord.Duration /= outputs.Count;
                energyRecord.EnergyData /= outputs.Count;
                output.EnergyRecords[i] = energyRecord;

                powerRecord.CTime /= outputs.Count;
                powerRecord.Duration /= outputs.Count;
                powerRecord.PowerData /= outputs.Count;
                output.PowerRecords[i] = powerRecord;
            }

            return output;
        }
    }
}
