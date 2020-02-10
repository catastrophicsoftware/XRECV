using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft;
using Newtonsoft.Json;

namespace XRECV
{
    /// <summary>
    /// Data structure to hold InfluxDB export config
    /// </summary>
    public sealed class InfluxDBConfig
    {
        public string host;
        public string username;
        public string password;
        public string database;
    }

    /// <summary>
    /// Data structure to hold primary XRECV application config
    /// </summary>
    public sealed class XRECVConfig
    {
        public string bindAddress;
        public int bindPort;
    }


    public sealed class ProgramConfig //this might get removed soon
    {
        private static ProgramConfig _inst = null;
        private static object instLock = new object();
        public static ProgramConfig inst
        {
            get
            {
                lock (instLock)
                {
                    if (_inst == null)
                        _inst = new ProgramConfig();
                    return _inst;
                }
            }
        }

        public XRECVConfig Config { get; private set; }

        public bool InfluxDBConfigPresent { get; private set; }
        public InfluxDBConfig InfluxDB { get; private set; }


        public ProgramConfig()
        {
            InfluxDB = null;
            Config = null;
        }

        /// <summary>
        /// Responsible for checking for, and loading the program's various config files
        /// </summary>
        public void CheckForConfigFiles()
        {
            if (File.Exists("xrecv.json"))
            {
                try
                {
                    Log.inst.Write("XRECV Config file located. loading...");
                    Config = JsonConvert.DeserializeObject<XRECVConfig>(File.ReadAllText("xrecv.json"));
                }
                catch (Exception ex)
                {
                    Log.inst.Write("ERROR LOADING XRECV.JSON!");
                    Log.inst.Write(ex.ToString());
                }
            }
            if(File.Exists("influxdb.json"))
            {
                try
                {
                    Log.inst.Write("InfluxDB export config file found. loading...");
                    InfluxDBConfigPresent = true;
                    InfluxDB = JsonConvert.DeserializeObject<InfluxDBConfig>(File.ReadAllText("influxdb.json"));
                    Log.inst.Write("Done Loading InfluxDB Config");

                    Log.inst.Write("Influx Host: " + InfluxDB.host);
                    Log.inst.Write("Influx Username: " + InfluxDB.username);
                    Log.inst.Write("Influx Password: " + InfluxDB.password);
                    Log.inst.Write("Influx Database Name: " + InfluxDB.database);
                }
                catch(Exception ex)
                {
                    Log.inst.Write("ERROR LOADING INFLUXDB CONFIG FILE!");
                    Log.inst.Write(ex.ToString());
                }
            }
        }
    }
}