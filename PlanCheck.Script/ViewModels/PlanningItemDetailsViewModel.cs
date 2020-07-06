using System.Collections.ObjectModel;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class PlanningItemDetailsViewModel : ViewModelBase
    {
        public bool IsBold { get; set; }
        public bool CC { get; set; }
        public string PlanningItemIdWithCourse { get; set; }
        public PlanningItem PlanningItemObject { get; set; }
        public ObservableCollection<PlanningItemDetailsViewModel> PlanSumComponentSummary { get; set; }
        public string ApprovalStatus { get; set; }
        public string IsPlanSum { get; set; }
        public string PlanName { get; set; }
        public string PlanCreated { get; set; }
        public string PlanFxDose { get; set; }
        public string PlanFractions { get; set; }
        public string PlanTotalDose { get; set; }
        public string PlanTarget { get; set; }
        public string PQMResult { get; set; }
        public string CCResult { get; set; }
        public string PCResult { get; set; }
        public string RPIResult { get; set; }
        public bool IsCCEnabled { get; set; }
    }
}
