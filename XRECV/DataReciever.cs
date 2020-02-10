using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;

namespace XRECV
{
    /// <summary>
    /// Mapping of X-Plane data output indexes
    /// Used to describe a set of data values send from X-Plane
    /// </summary>
    public enum XPlaneDataIndex : byte
    {
        FrameRate = 0,
        Times = 1,
        SimStats = 2,
        Speeds = 3,
        Mach_VVI_GLoad = 4,
        Weather = 5,
        AircraftAtmosphere = 6,
        SystemPressures = 7,
        JoystickAileronElevatorRudder = 8,
        ServoAvileronElevatorRudder = 138,
        ArtificialStabilityAileronElevatorRudder = 10,
        FlightControlsAileronElevatorRudder = 11,
        OtherFlightControls = 9,
        WingSweep_ThrustVectoring = 12,
        TrimFlapsStatsSpeedbrakes = 13,
        Gear_Brakes = 14,
        AngularMoments = 15,
        AngularVelocities = 16,
        PitchRollHeadings = 17,
        AOA_Sideslip_Paths = 18,
        MagneticCompas = 19,
        LatitudeLongitudeAltitude = 20,
        GearForces = 137,
    }

    public sealed class DataReciever
    {
        private UdpClient serverListener;
        private bool recieverRunning = true;

        private IPEndPoint _tempEndpoint = new IPEndPoint(IPAddress.Any, 0);

        private byte[] _tempBuffer = new byte[41]; //min size is 41. however will get re-allocated to hold as much data as is recieved
        private byte[] _tempHeader = new byte[4];
        private byte[] _tempIndex = new byte[4];
        private float[] _tempOutputValues = new float[8]; //all of these temp values are here to avoid rapid-fire repeat allocations

        InfluxDBExporter exporter;

        private int packetsRecieved;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bindIP">IP address to bind data collector server socket</param>
        /// <param name="bindPort">Port to bind data collector server socket</param>
        public DataReciever(int bindPort)
        {
            try
            {
                string bindIP = getLocalIP();

                Log.inst.Write("Initializing server socket...");
                Log.inst.Write("Bind IP: " + bindIP);
                Log.inst.Write("Bind Port: " + bindPort.ToString());

                IPEndPoint localBindEP = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);

                if (localBindEP == null)
                    Log.inst.Write("ERROR! UNABLE TO CREATE LOCAL IPEndPoint for UdpClient");

                serverListener = new UdpClient(localBindEP);
                Log.inst.Write("Done initializing server socket!");

                if (File.Exists("influxdb.json"))
                {
                    Log.inst.Write("Found InfluxDB Export config. Loading...");
                    InfluxDBConfig conf = JsonConvert.DeserializeObject<InfluxDBConfig>(File.ReadAllText("influxdb.json"));

                    InitializeInfluxDBExporter(conf.host,
                        conf.database,
                        conf.username,
                        conf.password);
                }
                else
                    Log.inst.Write("influxdb.json NOT FOUND");

                Timer packetPerSecondTimer = new Timer(new TimerCallback(packetPerSecondProc), null, 10000, 10000);

                Log.inst.Write("Data reciver initialization complete!");
            }
            catch(Exception ex)
            {
                ConsoleColor prevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Log.inst.Write("FATAL ERROR. UNABLE TO CREATE LISTENER SOCKET");
                Log.inst.Write(ex.ToString());

                Console.ForegroundColor = prevColor;
            }
        }

        private void InitializeInfluxDBExporter(string host, string database, string user, string password)
        {
            exporter = new InfluxDBExporter(host, user, database, password);
        }

        /// <summary>
        /// Begins primary data collection loop.
        /// This is a blocking function. It is the main execution loop of the data collector
        /// </summary>
        public void Run()
        {
            Log.inst.Write("Waiting for data...");
            while (recieverRunning)
            {
                _tempBuffer = serverListener.Receive(ref _tempEndpoint);
                packetsRecieved++;

                if(_tempEndpoint == null)
                {
                    Log.inst.Write("_tempEndpoint is null for an unknown reason!");
                    _tempEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 1); //populate
                }
                ProcessXPlaneDataPacket(_tempBuffer, _tempEndpoint);
            }
        }

        /// <summary>
        /// Simple function to compute packets per second
        /// </summary>
        /// <param name="param">unused, required for TimerCallback</param>
        private void packetPerSecondProc(object param)
        {
            int packetsPerSecond = packetsRecieved / 10;
            Log.inst.Write("PACKETS PER SECOND: " + packetsPerSecond);
            packetsRecieved = 0;
        }

        /// <summary>
        /// Processes and reads all data indexes and data values from an incoming X-Plane UDP data packet
        /// </summary>
        /// <param name="packet">raw bytes of data packet</param>
        private void ProcessXPlaneDataPacket(byte[] packet, IPEndPoint sender)
        {
            MemoryStream packetStream = new MemoryStream(packet);
            
            packetStream.Read(_tempHeader, 0, 4);
            if (Encoding.ASCII.GetString(_tempHeader) == "DATA") // valid x plane data packet
            {
                packetStream.ReadByte(); //advance past 5th junk byte in 5 byte header

                //we are now ready to begin reading data indexes and data values
                while (packetStream.Position < packetStream.Length)
                {
                    packetStream.Read(_tempIndex, 0, 4);

                    BinaryReader reader = new BinaryReader(packetStream);
                    
                    float[] data = ReadXPlaneDataValues(reader);

                    if (sender == null)
                    {
                        sender = new IPEndPoint(IPAddress.Parse("0.0.0.0"),1);
                        Log.inst.Write("ERROR: IPEndpoint \"sender\" is null!");
                    }
                    if (data == null)
                        Log.inst.Write("ERROR: float[] \"data\" is null!");

                    XPacket recievedPacket = new XPacket(sender, (XPlaneDataIndex)_tempIndex[0], DateTime.Now, data.ToArray());

                    exporter.WriteData(recievedPacket); //will only be able to write supported data types
                }
            }
            else
            {
                Log.inst.Write(" -- INVALID X PLANE PACKET");
                Log.inst.Write("SENDER: " + sender.ToString());
            }

            packetStream.Close();
        }

        /// <summary>
        /// Reads 8 floating point values from the data section of an X-Plane 11 data packet
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>List<float> containing 8 floating point values. if the value is -999 this indicates that X-Plane does not have real data for that value.
        /// Not all X-Plane data options populate all 8 floating point values in the packet</returns>
        private float[] ReadXPlaneDataValues(BinaryReader reader)
        {
            for(int i = 0; i < 8; ++i)
            {
                _tempOutputValues[i] = reader.ReadSingle();
            }
            return _tempOutputValues;
        }


        /// <summary>
        /// Retrieve local IP address
        /// </summary>
        /// <returns>local ip address as string</returns>
        public string getLocalIP()
        {
            using(Socket tempSocket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,0))
            {
                tempSocket.Connect("1.1.1.1", 65530);
                Log.inst.Write("Local IP Detected: " + tempSocket.LocalEndPoint.ToString());

                return tempSocket.LocalEndPoint.ToString().Split(':')[0]; //split off the random outbound port number
            }
        }
    }
}