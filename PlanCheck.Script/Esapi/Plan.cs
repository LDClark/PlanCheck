using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class Plan
    {
        public string CourseId { get; set; }
        public string PlanId { get; set; }
        public Beam[] Beams { get; set; }
        public string PlanType { get; set; }
    }
}