using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck
{
    public class PQMViewModel
    {
        public string TemplateId { get; set; }
        public string[] TemplateCodes { get; set; }
        public string[] TemplateAliases { get; set; }
        public ObservableCollection<StructureViewModel> StructureList { get; set; }
        public string StructVolume { get; set; }
        public string DVHObjective { get; set; }
        public string Goal { get; set; }
        public string Achieved { get; set; }
        public string ResultCompare1 { get; set; }
        public string ResultCompare2 { get; set; }
        public string ResultCompare3 { get; set; }
        public string Met { get; set; }
        public string Variation { get; set; }
        public string Priority { get; set; }
        public bool isCalculated { get; set; }
        public double AchievedPercentageOfGoal { get; set; }
        public string StructureName { get; set; }
        public string StructureNameWithCode { get; set; }
        public SolidColorBrush AchievedColor { get; set; }       
        public PlanningItemViewModel ActivePlanningItem { get; set;}
        public StructureViewModel SelectedStructure { get; set; }

    }
}
