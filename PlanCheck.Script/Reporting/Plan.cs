namespace PlanCheck.Reporting
{
    public class Plan
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public int Fractions { get; set; }
        public string[] Energies { get; set; }
        public Beam[] Beams { get; set; }
    }
}
