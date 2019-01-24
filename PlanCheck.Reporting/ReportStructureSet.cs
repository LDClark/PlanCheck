namespace PlanCheck.Reporting
{
    public class ReportStructureSet
    {
        public string Id { get; set; }
        public ReportImage Image { get; set; }
        public ReportStructure[] Structures { get; set; }
    }
}
