using System;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class Plan
    {
        public string CourseId { get; set; }
        public string PlanId { get; set; }
        public Beam[] Beams { get; set; }
        public string PlanType { get; set; }
        public DateTime PlanCreation { get; set; }
        public string PlanStructureSetId { get; set; }
        public string PlanImageId { get; set; }
        public DateTime PlanImageCreation { get; set; }
        public string PlanIdWithFractionation { get; set; }
    }
}