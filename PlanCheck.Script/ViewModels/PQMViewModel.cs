using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media;
using VMS.TPS.Common.Model.Types;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class PQMViewModel : ViewModelBase
    {
        public string TemplateId { get; set; }
        public string[] TemplateCodes { get; set; }
        public string[] TemplateAliases { get; set; }
        public ObservableCollection<StructureViewModel> StructureList { get; set; }
        public StructureSetViewModel StructureSet { get; set; }
        private string _structureVolume;
        public string StructureVolume
        {
            get { return _structureVolume; }
            set
            {
                _structureVolume = value;
                RaisePropertyChanged("StructureVolume");
            }
        }
        public string DVHObjective { get; set; }
        public string Goal { get; set; }
        private string _achieved;
        public string Achieved
        {
            get { return _achieved; }
            set
            {
                _achieved = value;
                RaisePropertyChanged("Achieved");
            }
        }
        private string _met;
        public string Met
        {
            get { return _met; }
            set
            {
                _met = value;
                RaisePropertyChanged("Met");
            }
        }
        public string Variation { get; set; }
        public string Priority { get; set; }
        public bool IsCalculated { get; set; }
        public string StructureName { get; set; }
        public string StructureNameWithCode { get; set; }
        private double _achievedPercentageOfGoal;
        public double AchievedPercentageOfGoal
        {
            get { return _achievedPercentageOfGoal; }
            set
            {
                _achievedPercentageOfGoal = value;
                RaisePropertyChanged("AchievedPercentageOfGoal");
            }
        }
        private SolidColorBrush _achievedColor;
        public SolidColorBrush AchievedColor
        {
            get { return _achievedColor; }
            set
            {
                _achievedColor = value;
                RaisePropertyChanged("AchievedColor");
            }
        }
        public PlanningItemViewModel ActivePlanningItem { get; set;}
        public string CourseId { get; set; }
        public string PlanId { get; set; }
        private StructureViewModel _selectedStructure;
        public StructureViewModel SelectedStructure
        {
            get { return _selectedStructure; }
            set
            {
                _selectedStructure = value;
            }
        }
    }
}
