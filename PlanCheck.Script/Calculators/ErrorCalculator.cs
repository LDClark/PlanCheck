using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class ErrorCalculator
    {
        public ObservableCollection<ErrorViewModel> Calculate(PlanningItemViewModel planningItem)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            errorGrid = GetPlanningItemErrors(planningItem);
            var structureSet = planningItem.StructureSet;
            var structureSetErrors = GetStructureSetErrors(structureSet);
            foreach (var structureSetError in structureSetErrors)  
                errorGrid.Add(structureSetError);
            return new ObservableCollection<ErrorViewModel>(errorGrid.OrderBy(x => x.Status));
        }

        public void AddNewRow(string description, string status, int severity, ObservableCollection<ErrorViewModel> errorGrid)
        {
            var errorColumns = new ErrorViewModel
            {
                Description = description,
                Status = status,
                Severity = severity
            };
            errorGrid.Add(errorColumns);
        }

        public ObservableCollection<ErrorViewModel> GetStructureSetErrors(StructureSetViewModel structureSet)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            foreach (var structure in structureSet.Structures)
            {
                try
                {
                    if (structure.Code == "NormalTissue")
                    {
                        if (structure.AssignedHU != 0)
                        {
                            var description = string.Format("Structure {0} has an assigned CT value of {1}.", structure.Id, structure.AssignedHU);
                            var severity = 1;
                            var status = "3 - OK";
                            AddNewRow(description,status, severity, errorGrid);
                        }
                        else
                        {
                            var description = string.Format("Structure {0} does not have an assigned CT value.", structure.Id);
                            var severity = 1;
                            var status = "1 - Warning";
                            AddNewRow(description, status, severity, errorGrid);
                        }
                    }
                }
                catch
                {
                }          
            }
            return errorGrid;
        }

        public ObservableCollection<ErrorViewModel> GetPlanningItemErrors(PlanningItemViewModel planningItem)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            string error;
            string errorStatus;
            int errorSeverity;

            var imageCreationDateTime = planningItem.StructureSet.ImageCreationDateTime;

            if ((planningItem.CreationDateTime - imageCreationDateTime).TotalDays > 21)
            {
                error = string.Format("CT and structure data ({0}) is {1} days older than plan creation date ({2}) and outside of 21 days.", imageCreationDateTime, (planningItem.CreationDateTime - imageCreationDateTime).TotalDays.ToString("0"), planningItem.CreationDateTime);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("CT and structure data ({0}) is {1} days older than plan creation date ({2}) and within 21 days.", imageCreationDateTime, (planningItem.CreationDateTime - imageCreationDateTime).TotalDays.ToString("0"), planningItem.CreationDateTime);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            if (planningItem.IsDoseValid)
            {
                if (planningItem.DoseMax3D >= 115)
                {
                    error = string.Format("Dose maximum is {0}.", planningItem.DoseMax3D.ToString("0.0") + " %");
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                if (planningItem.DoseMax3D >= 110 && planningItem.DoseMax3D < 115)
                {
                    error = string.Format("Dose maximum is {0}.", planningItem.DoseMax3D.ToString("0.0") + " %");
                    errorStatus = "2 - Variation";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                if (planningItem.DoseMax3D >= 100 && planningItem.DoseMax3D < 110)
                {
                    error = string.Format("Dose maximum {0}.", planningItem.DoseMax3D.ToString("0.0") + " %");
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }

            if (planningItem.Id == planningItem.Name)
            {
                error = string.Format("Plan ID ({0}) matches plan Name ({1}).", planningItem.Id, planningItem.Name);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("Plan ID ({0}) does not match plan Name ({1}).", planningItem.Id, planningItem.Name);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            foreach (var structure in planningItem.StructureSet.Structures)
            {
                if (structure.Id.Contains("CouchSurface") == true)
                {
                    double lowerLimitHU = -650;
                    double upperLimitHU = -425;
                    if (structure.AssignedHU <= upperLimitHU || structure.AssignedHU >= lowerLimitHU)
                    {
                        error = string.Format("Structure {0} has assigned HU of {1} and is within limit of {2} to {3}.", structure, structure.AssignedHU, upperLimitHU, lowerLimitHU);
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Structure {0} has assigned HU of {1} and is outside limit of {2} to {3}.", structure, structure.AssignedHU, upperLimitHU, lowerLimitHU);
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    break;
                }
            }

            if (planningItem.TargetVolumeId == "")
            {
                error = string.Format("Plan {0} does not have a target volume assigned.", planningItem.Id);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("Plan {0} has a target volume assigned.", planningItem.Id);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            return errorGrid;
        }
    }
}
