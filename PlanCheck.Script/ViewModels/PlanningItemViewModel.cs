using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class PlanningItemViewModel : ViewModelBase
    {
        public string PlanningItemId { get; set; }
        public string PlanningItemCourse { get; set; }
        public string PlanningItemIdWithCourse { get; set; }
        public string PlanningItemIdWithCourseAndType { get; set; }
        public string PlanningItemType { get; set; }
        public PlanningItem PlanningItemObject { get; set; }
        public DateTime Creation { get; set; }
        public StructureSet PlanningItemStructureSet { get; set; }
        public Image PlanningItemImage { get; set; }
        
        public PlanningItemViewModel(PlanningItem planningItem)
        {
            if (planningItem is PlanSetup)
            {
                PlanSetup planSetup = (PlanSetup)planningItem;
                PlanningItemId = planSetup.Id;
                PlanningItemCourse = planSetup.Course.Id;
                PlanningItemIdWithCourse = PlanningItemCourse + "/" + PlanningItemId;             
                PlanningItemType = "Plan";
                PlanningItemIdWithCourseAndType = PlanningItemCourse + "/" + PlanningItemId + " (" + PlanningItemType + ")";
                PlanningItemObject = planSetup;
                PlanningItemStructureSet = planSetup.StructureSet;
                PlanningItemImage = planSetup.StructureSet.Image;
                Creation = (DateTime) planSetup.CreationDateTime;
            }
            if (planningItem is PlanSum)
            {
                PlanSum planSum = (PlanSum)planningItem;
                PlanningItemId = planSum.Id;
                PlanningItemCourse = planSum.Course.Id;
                PlanningItemIdWithCourse = PlanningItemCourse + "/" + PlanningItemId;
                PlanningItemType = "PlanSum";
                PlanningItemIdWithCourseAndType = PlanningItemCourse + "/" + PlanningItemId + " (" + PlanningItemType + ")";
                PlanningItemObject = planSum;
                PlanningItemStructureSet = planSum.StructureSet;
                PlanningItemImage = planSum.StructureSet.Image;
                Creation = (DateTime)planSum.HistoryDateTime;
            }
        }
    }
}
