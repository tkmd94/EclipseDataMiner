namespace EclipseDataMiner
{
    public enum DQPtype
    {
        Dose,
        Volume,
        DoseComplement,
        ComplementVolume
    }
    public enum IOUnit
    {
        Relative,
        Absolute        
    }

      public class DQP
    {
        // Structure name
        public string structureName { get; set; }
        // DQP type
        public DQPtype DQPtype { get; set; }
        public double DQPvalue { get; set; }

        // IO unit
        public IOUnit InputUnit { get; set; }
        public IOUnit OutputUnit { get; set; }
    }
}
