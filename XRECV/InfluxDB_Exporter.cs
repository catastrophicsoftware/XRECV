using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Linq;
using InfluxDB;
using InfluxDB.Collector;

namespace XRECV
{
    public sealed class InfluxDBExporter
    {
        private MetricsCollector FDR;

        private Dictionary<string, object> _tempDataDict = new Dictionary<string, object>();

        public InfluxDBExporter(string DBHost, string DBUser,string DBName, string DBPassword)
        {
            ConnectToInfluxDB(DBHost, DBUser, DBName, DBPassword);
        }

        private void ConnectToInfluxDB(string host,string user,string database, string password)
        {
            try
            {
                FDR = new CollectorConfiguration().Tag.With("flight", "data").Batch.AtInterval(TimeSpan.FromSeconds(1)).WriteTo.InfluxDB(host, database, user, password).CreateCollector();
            }
            catch(Exception ex)
            {
                Log.inst.Write("ERROR CONNECTING TO INFLUXDB");
                Log.inst.Write(ex.ToString());
            }
        }

        public void WriteData(XPacket data)
        {
            //hacky bugfix
            if ((data.XPlaneDataIndex == XPlaneDataIndex.LatitudeLongitudeAltitude) && (data.DataValues[0] == 0 && data.DataValues[1] == 0))
            {
                Log.inst.Write("JUNK DATA DISCARDED! -- LAT/LON NULL ISLAND POSITION latlon(0,0) DISCARDED.");
                return; //discard packet. This is a junk packet that will plot a point to null island
            }
                

            switch(data.XPlaneDataIndex) //TODO: refactor this to use the master mapping list of data value names
            {
                case XPlaneDataIndex.LatitudeLongitudeAltitude:
                    {
                        FDR.Write("position", new Dictionary<string, object> //TODO: make sure that the measurements are sent to different locations for different users
                        {
                            {"lat" , data.DataValues[0]},
                            {"lon" , data.DataValues[1]},
                            {"alt_msl" , data.DataValues[2]},
                            {"alt_agl" , data.DataValues[3]},
                            {"on_runwy" , data.DataValues[4]},
                            {"alt_ind", data.DataValues[5]},
                            {"lat_origin", data.DataValues[6]},
                            {"lon_origin", data.DataValues[7]}
                        });
                        break;
                    }
                case XPlaneDataIndex.Mach_VVI_GLoad:
                    {
                        FDR.Write("MachGLoad", new Dictionary<string, object>
                        {
                            {"mach" , data.DataValues[0]},
                            {"fpm", data.DataValues[2] },
                            {"gload_norm" , data.DataValues[4] },
                            {"gload_axial" , data.DataValues[5] },
                            {"gload_side" , data.DataValues[6] },
                        });
                        break;
                    }
                case XPlaneDataIndex.JoystickAileronElevatorRudder:
                    {
                        FDR.Write("joystick_ail_elev_rudd", new Dictionary<string, object>
                        {
                            {"elev" , data.DataValues[0]},
                            {"ail", data.DataValues[1] },
                            {"rudd" , data.DataValues[2] },
                        });
                        break;
                    }
                case XPlaneDataIndex.FlightControlsAileronElevatorRudder:
                    {
                        FDR.Write("flightcontrols_ail_elev_rudd", new Dictionary<string, object>
                        {
                            {"elev" , data.DataValues[0]},
                            {"ail", data.DataValues[1] },
                            {"rudd" , data.DataValues[2] },
                        });
                        break;
                    }
                case XPlaneDataIndex.TrimFlapsStatsSpeedbrakes:
                    {
                        FDR.Write("trim_flaps_stats_speedbrakes", new Dictionary<string, object>
                        {
                            {"elev_trim" , data.DataValues[0]},
                            {"ail_trim", data.DataValues[1] },
                            {"rudd_trim" , data.DataValues[2] },
                            {"flap_pos", data.DataValues[4] },
                            {"slat_pos", data.DataValues[5] },
                            {"sbrak_pos", data.DataValues[7] }
                        });
                        break;
                    }
                case XPlaneDataIndex.Speeds:
                    {
                        FDR.Write("speeds", new Dictionary<string, object>
                        {
                            {"ias" , data.DataValues[0]},
                            {"eas", data.DataValues[1] },
                            {"tas" , data.DataValues[2] },
                            {"gs", data.DataValues[3] },
                            {"ias_mph", data.DataValues[5] },
                            {"tas_mph", data.DataValues[6] }
                        });
                        break;
                    }
                case XPlaneDataIndex.GearForces:
                    {
                        FDR.Write("gear_forces", new Dictionary<string, object>
                        {
                            {"norm" , data.DataValues[0]},
                            {"axial", data.DataValues[1] },
                            {"side" , data.DataValues[2] },
                            {"L", data.DataValues[3] },
                            {"M", data.DataValues[4] },
                            {"N", data.DataValues[5] }
                        });
                        break;
                    }
                default:
                    {
                        _tempDataDict.Clear();

                        for(int i = 0; i < 8; ++i)
                        {
                            _tempDataDict.Add(i.ToString(), data.DataValues[i]);
                        }

                        FDR.Write(data.XPlaneDataIndex.ToString(), _tempDataDict); 
                        //This will allow for data to still get written out to the DB even if we haven't
                        //mapped the data value names to the data value indices. For this data. The user will have to
                        //manually label it

                        break;
                    }
            }
        }
    }
}