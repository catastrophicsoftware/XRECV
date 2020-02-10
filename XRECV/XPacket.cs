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

namespace XRECV
{
    /// <summary>
    /// Stores a single UDP output data packet from X Plane
    /// </summary>
    public sealed class XPacket
    {
        public IPEndPoint Sender { get { return sender; } }
        public XPlaneDataIndex XPlaneDataIndex { get { return dataIndex; } }
        public DateTime TimeRecieved { get { return timeRecieved; } }
        public float[] DataValues { get { return dataValues; } }

        private IPEndPoint sender;
        private XPlaneDataIndex dataIndex;
        private DateTime timeRecieved;
        private float[] dataValues;

        /// <summary>
        /// Gets a data value by name. For example "lat" field from the X-Plane "LatitudeLongitudeAltitude" data set
        /// </summary>
        /// <param name="xplaneDataValueName"></param>
        /// <returns>data value</returns>
        public float this[string xplaneDataValueName]
        {
            get
            {
                return DataValues[XPlaneDataMappings.mappings[XPlaneDataIndex.ToString()][xplaneDataValueName]];
            }
        }

        /// <summary>
        /// Initializes packet data structure
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="DataIndex"></param>
        /// <param name="RecievedTime"></param>
        /// <param name="DataValues"></param>
        public XPacket(IPEndPoint Sender, XPlaneDataIndex DataIndex, DateTime RecievedTime, float[] DataValues)
        {
            sender = Sender;
            dataIndex = DataIndex;
            timeRecieved = RecievedTime;
            dataValues = DataValues;
        }
    }

    /// <summary>
    /// Holds a master mapping of all XPlane 11 data output values, and their 8 named subvalues (not all of which are used for every output)
    /// </summary>
    public static class XPlaneDataMappings
    {
        public static Dictionary<string, Dictionary<string, int>> mappings = new Dictionary<string, Dictionary<string, int>>()
        {
            {"LatitudeLongitudeAltitude", new Dictionary<string, int>()
                {
                    {"lat", 0 },
                    {"lon", 1 },
                    {"alt_msl", 2 },
                    {"alt_agl",3 },
                    {"on_runwy" , 4},
                    {"alt_ind", 5},
                    {"lat_origin", 6},
                    {"lon_origin", 7}
                }
            },
            {"MachGLoad",new Dictionary<string, int>()
                {
                    {"mach" , 0},
                    {"fpm", 2 },
                    {"gload_norm" , 4 },
                    {"gload_axial" , 5 },
                    {"gload_side" , 6 },
                }
            },
            {"JoystickAileronElevatorRudder",new Dictionary<string, int>()
                {
                    {"elev" , 0},
                    {"ail", 1 },
                    {"rudd" , 2 },
                }
            },
            {"FlightControlsAileronElevatorRudder",new Dictionary<string, int>()
                {
                    {"elev" , 0},
                    {"ail", 1 },
                    {"rudd" , 2 },
                }
            }
            //TODO: The rest of these
        };
    }
}