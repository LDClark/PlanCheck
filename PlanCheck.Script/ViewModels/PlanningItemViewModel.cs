using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck
{
    public class PlanningItemViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CourseId { get; set; }
        public string PatientId { get; set; }
        public string IdWithCourse { get; set; }
        public string IdWithCourseAndType { get; set; }
        public string Type { get; set; }
        public PlanningItem Object { get; set; }
        public DateTime CreationDateTime { get; set; }
        public StructureSetViewModel StructureSet { get; set; }
        public string StructureSetId { get; set; }
        public Image Image { get; set; }
        public string PlanImageId { get; set; }
        public DateTime PlanImageCreation { get; set; }
        public string PlanIdWithFractionation { get; set; }
        public double TotalDose { get; set; }
        public bool IsDoseValid { get; set; }
        public string TargetVolumeId { get; set; }
        public double DoseMax3D { get; set; }
        public VVector DoseMax3DLocation { get; set; }
        public Beam[] Beams { get; set; }

        public PlanningItemViewModel(PlanningItem planningItem)
        {
            if (planningItem is PlanSetup)
            {
                PlanSetup planSetup = (PlanSetup)planningItem;
                Id = planSetup.Id;
                Name = planSetup.Name;
                CourseId = planSetup.Course.Id;
                PatientId = planSetup.Course.Patient.Id;
                IdWithCourse = CourseId + "/" + Id;             
                Type = "Plan";
                IdWithCourseAndType = CourseId + "/" + Id + " (" + Type + ")";
                Object = planSetup;
                StructureSet = new StructureSetViewModel(planSetup.StructureSet);
                Image = planSetup.StructureSet.Image;
                CreationDateTime = (DateTime) planSetup.CreationDateTime;
                TargetVolumeId = planSetup.TargetVolumeID;
                TotalDose = planSetup.TotalDose.Dose;
                IsDoseValid = planSetup.IsDoseValid;
                if (IsDoseValid)
                {
                    DoseMax3D = planSetup.Dose.DoseMax3D.Dose;
                    DoseMax3DLocation = planSetup.Dose.DoseMax3DLocation;
                }
            }
            if (planningItem is PlanSum)
            {
                PlanSum planSum = (PlanSum)planningItem;
                Id = planSum.Id;
                CourseId = planSum.Course.Id;
                IdWithCourse = CourseId + "/" + Id;
                PatientId = planSum.Course.Patient.Id;
                Type = "PlanSum";
                IdWithCourseAndType = CourseId + "/" + Id + " (" + Type + ")";
                Object = planSum;
                StructureSet = new StructureSetViewModel(planSum.StructureSet);
                Image = planSum.StructureSet.Image;
                CreationDateTime = (DateTime)planSum.HistoryDateTime;
                foreach (var planSetup in planSum.PlanSetups)
                {
                    TotalDose += planSetup.TotalDose.Dose;
                    TargetVolumeId = planSetup.TargetVolumeID;
                }
                IsDoseValid = planSum.IsDoseValid();
                if (IsDoseValid)
                {
                    DoseMax3D = planSum.Dose.DoseMax3D.Dose;
                    DoseMax3DLocation = planSum.Dose.DoseMax3DLocation;
                }
            }
        }

        static public ObservableCollection<PlanningItemViewModel> GetPlanningItemList(IEnumerable<PlanSetup> planSetupsInScope, IEnumerable<PlanSum> planSumsInScope)
        {
            var PlanningItemComboBoxList = new ObservableCollection<PlanningItemViewModel>();
            foreach (PlanSetup planSetup in planSetupsInScope)
            {
                var planningItemViewModel = new PlanningItemViewModel(planSetup);
                PlanningItemComboBoxList.Add(planningItemViewModel);
            }

            foreach (PlanSum planSum in planSumsInScope)
            {
                var planningItemViewModel = new PlanningItemViewModel(planSum);
                PlanningItemComboBoxList.Add(planningItemViewModel);
            }
            return PlanningItemComboBoxList;
        }
    }
}
