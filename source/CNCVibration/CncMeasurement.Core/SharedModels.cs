using System;

namespace CncMeasurement.Core.models
{
    // general information about the database collections
    public class DBinfo
    {
        public string[] Collections;
    }
    // General measurement data
    public class MeasurementMetadata
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public GraphMetadata[] Graphs { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
    }
    // for storing the informations about a .TDMS graph
    public class GraphMetadata
    {

        public string Description { get; set; }
        public string Xaxis { get; set; }
        public string Yaxis { get; set; }
        public string FilePath { get; set; }
    }
    public class BriefMeasurementInfo
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}
