using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck
{
    public class PQMSummaryViewModel : ViewModelBase
    {
        public string TemplateId { get; set; }
        public string[] TemplateCodes { get; set; }
        public string[] TemplateAliases { get; set; }
        public ObservableCollection<StructureViewModel> StructureList { get; set; }
        public string StructVolume { get; set; }
        public string DVHObjective { get; set; }
        public string Goal { get; set; }
        public string Achieved { get; set; }
        public string AchievedComparison { get; set; }
        public string Met { get; set; }
        public string Variation { get; set; }
        public string Priority { get; set; }
        public bool isCalculated { get; set; }
        public double AchievedPercentageOfGoal { get; set; }
        public string StructureName { get; set; }
        public string StructureNameWithCode { get; set; }
        public SolidColorBrush AchievedColor { get; set; }       
        public PlanningItemViewModel ActivePlanningItem { get; set;}

        public StructureViewModel _Structure;
        public StructureViewModel Structure
        {
            get { return _Structure; }
            set
            {
                _Structure = value;
                if (Structure != null || Goal != null) //Structure is found and not null
                {
                    var calculator = new PQMSummaryCalculator();
                    StructVolume = _Structure.VolumeValue;
                    Achieved = calculator.CalculateMetric(ActivePlanningItem.PlanningItemStructureSet, _Structure, ActivePlanningItem, DVHObjective);
                    Met = calculator.EvaluateMetric(Achieved, Goal, Variation);
                    NotifyPropertyChanged("Structure");
                    var tuple = Calculators.PQMColors.GetAchievedColor(Structure.Structure, Goal, DVHObjective, Achieved);
                    AchievedColor = tuple.Item1;
                    AchievedPercentageOfGoal = tuple.Item2;
                }
            }
        }
    }
}
