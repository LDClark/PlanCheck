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
        public ObservableCollection<ErrorViewModel> Calculate(PlanningItem planningItem)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            if (planningItem is PlanSetup)
            {
                PlanSetup planSetup = (PlanSetup)planningItem;
                errorGrid = GetPlanSetupErrors(planSetup);             
            }
            if (planningItem is PlanSum)
            {
                PlanSum planSum = (PlanSum)planningItem;
                foreach (PlanSetup planSetup in planSum.PlanSetups)
                {
                    errorGrid = GetPlanSetupErrors(planSetup);
                }
            }
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

        public ObservableCollection<ErrorViewModel> GetStructureSetErrors(StructureSet structureSet)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            foreach (var structure in structureSet.Structures)
            {
                try
                {
                    if (structure.StructureCodeInfos.FirstOrDefault().Code == "NormalTissue")
                    {
                        ;
                        if (structure.GetAssignedHU(out double huValue))
                        {
                            var description = string.Format("Structure {0} has an assigned CT value of {1}.", structure.Id, huValue);
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

                    if (structure.GetNumberOfSeparateParts() > 1)
                    {
                        var description = string.Format("Structure {0} has {1} separate parts.", structure.Id, structure.GetNumberOfSeparateParts());
                        var severity = 1;
                        var status = "1 - Warning";
                        AddNewRow(description, status, severity, errorGrid);
                    }
                }
                catch
                {

                }
                
            }
            return errorGrid;
        }

        public ObservableCollection<ErrorViewModel> GetPlanSumRxErrors(PlanSum planSum)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            double totalRxDose = 0;
            string error;
            string errorStatus;
            int errorSeverity;
            var doseValueList = new List<string>();
            var doseNameList = new List<string>();
            var siteNameList = new List<string>();
            foreach (PlanSetup planSetup in planSum.PlanSetups.OrderBy(x => x.CreationDateTime))
            {
                totalRxDose += planSetup.TotalDose.Dose;
                
                if (planSetup.Id.Contains('_'))
                {
                    var doseName = planSetup.Id.Split('_')[1];
                    doseName = Regex.Match(doseName, @"\d+").Value;
                    doseNameList.Add(doseName);
                    siteNameList.Add(planSetup.Id.Split('_')[0]);
                    doseValueList.Add(totalRxDose.ToString());
                }
            }
            int i = 0;
            foreach (string doseName in doseNameList)
            {
                if (doseName == doseValueList[i])
                {
                    error = string.Format("Plan name {0} matches Rx dose of {1} cGy.", siteNameList[i] + "_" + doseName, doseValueList[i]);
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                else
                {
                    error = string.Format("Plan name {0} does NOT match Rx dose of {1} cGy.", siteNameList[i]+ "_" + doseName, doseValueList[i]);
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                i++;
            }

            var numbersInString = Regex.Match(planSum.Id, @"\d+").Value;
            if (numbersInString != "") //contains no numbers at all
            {
                if (Int32.Parse(numbersInString).ToString() == totalRxDose.ToString())
                {
                    error = string.Format("PlanSum name {0} matches Rx dose of {1} cGy.", planSum.Id, totalRxDose);
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                else
                {
                    error = string.Format("PlanSum name {0} does NOT match Rx dose of {1} cGy.", planSum.Id, totalRxDose);
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }
            return errorGrid;
        }

        public ObservableCollection<ErrorViewModel> GetPlanSetupErrors(PlanSetup planSetup)
        {
            var errorGrid = new ObservableCollection<ErrorViewModel>();
            string error;
            string errorStatus;
            int errorSeverity;
            double totalRxDose;
            var doseValueList = new List<string>();
            var doseNameList = new List<string>();
            var siteNameList = new List<string>();

            bool couchFound = false;
            bool rtLatSetupFound = false;
            bool ltLatSetupFound = false;
            bool paSetupFound = false;
            bool apSetupFound = false;
            bool cbctSetupFound = false;

            totalRxDose = planSetup.TotalDose.Dose;

            if (planSetup.Id.Contains('_'))
            {
                if (planSetup.Id.Split('_')[1].Contains("#"))
                    doseNameList.Add(planSetup.Id.Split('_')[1].Split('#')[0]);
                else
                    doseNameList.Add(planSetup.Id.Split('_')[1]);
                siteNameList.Add(planSetup.Id.Split('_')[0]);
                doseValueList.Add(totalRxDose.ToString());
            }
            int i = 0;
            bool planSetupInPlanSum = false;
            foreach (PlanSum planSum in planSetup.Course.PlanSums)
            {
                foreach (PlanSetup ps in planSum.PlanSetups)
                {
                    if (ps == planSetup)
                    {
                        planSetupInPlanSum = true;
                    }
                }
            }
            if (planSetupInPlanSum == false)
            {
                foreach (string doseName in doseNameList)
                {
                    if (doseName.Contains(":") == true && (doseName.Split(':')[0] == doseValueList[i]))  //a revision
                    {
                        error = string.Format("Plan setup name {0} matches Rx dose of {1} cGy.", siteNameList[i] + "_" + doseName, doseValueList[i]);
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    if (doseName == doseValueList[i])
                    {
                        error = string.Format("Plan setup name {0} matches Rx dose of {1} cGy.", siteNameList[i] + "_" + doseName, doseValueList[i]);
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Plan setup name {0} does NOT match Rx dose of {1} cGy.", siteNameList[i] + "_" + doseName, doseValueList[i]);
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    i++;
                }
            }

            if ((planSetup.CreationDateTime.Value - planSetup.StructureSet.Image.CreationDateTime.Value).TotalDays > 21)
            {
                error = string.Format("CT and structure data ({0}) is {1} days older than plan creation date ({2}) and outside of 21 days.", planSetup.StructureSet.Image.CreationDateTime.Value, (planSetup.CreationDateTime.Value - planSetup.StructureSet.Image.CreationDateTime.Value).TotalDays.ToString("0"), planSetup.CreationDateTime.Value);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("CT and structure data ({0}) is {1} days older than plan creation date ({2}) and within 21 days.", planSetup.StructureSet.Image.CreationDateTime.Value, (planSetup.CreationDateTime.Value - planSetup.StructureSet.Image.CreationDateTime.Value).TotalDays.ToString("0"), planSetup.CreationDateTime.Value);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            if (planSetup.Dose != null)
            {
                if (planSetup.TargetVolumeID != "")
                {
                    try
                    {
                        Structure target = planSetup.StructureSet.Structures.First(x => x.Id == planSetup.TargetVolumeID);
                        error = target.IsPointInsideSegment(planSetup.Dose.DoseMax3DLocation) ?
                        $"Dose maximum {planSetup.Dose.DoseMax3D} is inside {target.Id}." : $"Dose maximum {planSetup.Dose.DoseMax3D} not in target.";
                        errorStatus = target.IsPointInsideSegment(planSetup.Dose.DoseMax3DLocation) ?
                        "3 - OK" : "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    catch
                    {

                    }
                }
                if (planSetup.Dose.DoseMax3D.Dose >= 115)
                {
                    error = string.Format("Dose maximum is {0}.", planSetup.Dose.DoseMax3D.Dose.ToString("0.0") + " %");
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                if (planSetup.Dose.DoseMax3D.Dose >= 110 && planSetup.Dose.DoseMax3D.Dose < 115)
                {
                    error = string.Format("Dose maximum is {0}.", planSetup.Dose.DoseMax3D.Dose.ToString("0.0") + " %");
                    errorStatus = "2 - Variation";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                if (planSetup.Dose.DoseMax3D.Dose >= 100 && planSetup.Dose.DoseMax3D.Dose < 110)
                {
                    error = string.Format("Dose maximum {0}.", planSetup.Dose.DoseMax3D.Dose.ToString("0.0") + " %");
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }

            if (planSetup.Id == planSetup.Name)
            {
                error = string.Format("Plan ID ({0}) matches plan Name ({1}).", planSetup.Id, planSetup.Name);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("Plan ID ({0}) does not match plan Name ({1}).", planSetup.Id, planSetup.Name);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            if (planSetup.OptimizationSetup != null)
            {
                foreach (OptimizationObjective obj in planSetup.OptimizationSetup.Objectives)
                {
                    if (obj is OptimizationPointObjective)
                    {
                        OptimizationPointObjective opo = obj as OptimizationPointObjective;
                        double priority = opo.Priority;


                        if (priority == 0 || priority > 200)
                        {
                            error = string.Format("Structure {0} has a priority of {1} and is not within the range of 0 to 200.", opo.StructureId, opo.Priority);
                            errorStatus = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Structure {0} has a priority of {1} and is within the range of 0 to 200.", opo.StructureId, opo.Priority);
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                }
            }


            foreach (Structure structure in planSetup.StructureSet.Structures)
            {
                if (structure.Id.Contains("CouchSurface") == true)
                {
                    structure.GetAssignedHU(out double assignedHU);
                    double lowerLimitHU = -650;
                    double upperLimitHU = -425;
                    if (assignedHU <= upperLimitHU || assignedHU >= lowerLimitHU)
                    {
                        error = string.Format("Structure {0} has assigned HU of {1} and is within limit of {2} to {3}.", structure, assignedHU, upperLimitHU, lowerLimitHU);
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Structure {0} has assigned HU of {1} and is outside limit of {2} to {3}.", structure, assignedHU, upperLimitHU, lowerLimitHU);
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    couchFound = true;
                    break;
                }
            }

            if (planSetup.TargetVolumeID == "")
            {
                error = string.Format("Plan {0} does not have a target volume assigned.", planSetup.Id);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("Plan {0} has a target volume assigned.", planSetup.Id);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            if (planSetup.Beams.FirstOrDefault().MLCPlanType.ToString() == "DoseDynamic")
            {
                if (couchFound == false)
                {
                    error = string.Format("Plan {0} is IMRT but there is no couch inserted (okay if 6DoF headholder present).", planSetup.Id);
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                else
                {
                    error = string.Format("Plan {0} is IMRT and there is a couch inserted (except if 6DoF headholder present).", planSetup.Id);
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }

            if (planSetup.Beams.FirstOrDefault().MLCPlanType.ToString() == "VMAT")
            {
                if (couchFound == false)
                {
                    error = string.Format("Plan {0} is VMAT but there is no couch inserted (okay if 6DoF headholder present).", planSetup.Id);
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                else
                {
                    error = string.Format("Plan {0} is VMAT and there is a couch inserted (except if 6DoF headholder present).", planSetup.Id);
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }

            

            foreach (Beam b in planSetup.Beams)
            {
                if (b.MLCPlanType.ToString() == "VMAT")
                {
                    //double[] CPList = new double[b.ControlPoints.Count];
                    int CPsWithDoseRateMaxed = 0;
                    int CPsWithLargeSpeedChange = 0;
                    double mu = b.Meterset.Value;
                    double doserateSum = 0;
                    double maxGantrySpeed = 0.8 * 360.0 / 60.0; //default value is 0.8 RPM. There may be an operating limit for this.
                    for (int n = 1; n < b.ControlPoints.Count - 1; n++)
                    {
                        var cpPrev = b.ControlPoints[n - 1];
                        var cp = b.ControlPoints[n];
                        var cpNext = b.ControlPoints[n + 1];
                        double deltaAngle = GetDeltaAngle(cpPrev.GantryAngle, cp.GantryAngle);
                        double deltaAngleNext = GetDeltaAngle(cp.GantryAngle, cpNext.GantryAngle);
                        double deltaMU = mu * (cp.MetersetWeight - cpPrev.MetersetWeight);
                        double deltaMUNext = mu * (cpNext.MetersetWeight - cp.MetersetWeight);
                        double segmentDeliveryTime = CalculateSDT(deltaAngle, deltaMU, maxGantrySpeed, b.DoseRate / 60.0);
                        double segmentDeliveryTimeNext = CalculateSDT(deltaAngleNext, deltaMUNext, maxGantrySpeed, b.DoseRate / 60.0);
                        double doserate = deltaMU / segmentDeliveryTime * 60.0;
                        doserateSum += doserate;
                        double gantrySpeed = deltaAngle / segmentDeliveryTime;
                        double gantrySpeedNext = deltaAngleNext / segmentDeliveryTimeNext;
                        double gantrySpeedDelta = gantrySpeedNext - gantrySpeed;
                        double muPerDeg = deltaMU / deltaAngle;

                        if (doserate == b.DoseRate)
                            CPsWithDoseRateMaxed += 1;
                        if (gantrySpeedDelta > 0.1)
                            CPsWithLargeSpeedChange += 1;

                    }
                    double doserateAvg = doserateSum / b.ControlPoints.Count;
                    if (CPsWithDoseRateMaxed > 1)
                    {
                        error = string.Format("Field {0} has {1} of {2} control points with max dose rate {3} MU/min.  Average dose rate is {4} MU/min.", b.Id, CPsWithDoseRateMaxed, b.ControlPoints.Count, b.DoseRate, doserateAvg.ToString("0.0"));
                        errorStatus = "2 - Variation";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Field {0} has {1} of {2} control points with max dose rate {3} MU/min.  Average doserate is {4} MU/min.", b.Id, CPsWithDoseRateMaxed, b.ControlPoints.Count, b.DoseRate, doserateAvg.ToString("0.0"));
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    if (CPsWithLargeSpeedChange > 1)
                    {
                        error = string.Format("Field {0} has {1} of {2} control points with gantry speed change > 0.1 deg/s.", b.Id, CPsWithLargeSpeedChange, b.ControlPoints.Count);
                        errorStatus = "2 - Variation";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Field {0} has {1} of {2} control points with gantry speed change > 0.1 deg/s.", b.Id, CPsWithLargeSpeedChange, b.ControlPoints.Count);
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                }

                double gantryAngle = b.ControlPoints.First().GantryAngle;
                double tableAngle = 0;
                bool subfield1Found = false;
                bool subfield2Found = false;
                bool subfield3Found = false;
                if (b.ControlPoints.First().PatientSupportAngle == 0)
                    tableAngle = b.ControlPoints.First().PatientSupportAngle;
                else
                    tableAngle = 360 - b.ControlPoints.First().PatientSupportAngle;
                string fieldId = b.Id;
                fieldId = fieldId.Replace(" ", "");
                if (b.IsSetupField != true)
                {
                    if (b.Name.Contains("Subfield 1"))
                        subfield1Found = true;
                    if (b.Name.Contains("Subfield 2"))
                        subfield2Found = true;
                    if (b.Name.Contains("Subfield 3"))
                        subfield3Found = true;
                    if (tableAngle == 0)
                    {
                        if (subfield1Found == true)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString() + "_1"))
                            {
                                error = string.Format("Field {0} matches name format 'G{1}_1'.", b.Id, gantryAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1}_1'.", b.Id, gantryAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                        if (subfield2Found == true)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString() + "_2"))
                            {
                                error = string.Format("Field {0} matches name format 'G{1}_2'.", b.Id, gantryAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1}_2'.", b.Id, gantryAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                        if (subfield3Found == true)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString() + "_3"))
                            {
                                error = string.Format("Field {0} matches name format 'G{1}_3'.", b.Id, gantryAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1}_3'.", b.Id, gantryAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                        if (subfield1Found == false && subfield2Found == false && subfield3Found == false)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString()))
                            {
                                error = string.Format("Field {0} matches name format 'G{1}'.", b.Id, gantryAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1}'.", b.Id, gantryAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                    }
                    if (tableAngle != 0)
                    {
                        if (subfield1Found == true)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString() + " T" + tableAngle.ToString() +  "_1"))
                            {
                                error = string.Format("Field {0} matches name format 'G{1} T{2}_1'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1} T{2}_1'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                        if (subfield2Found == true)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString() + "_2"))
                            {
                                error = string.Format("Field {0} matches name format 'G{1} T{2}_2'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1} T{2}_2'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                        if (subfield3Found == true)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString() + "_3"))
                            {
                                error = string.Format("Field {0} matches name format 'G{1} T{2}_3'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1} T{2}_3'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                        if (subfield1Found == false && subfield2Found == false && subfield3Found == false)
                        {
                            if (fieldId == ("G" + gantryAngle.ToString()))
                            {
                                error = string.Format("Field {0} matches name format 'G{1} T{2}'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "3 - OK";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                            else
                            {
                                error = string.Format("Field {0} does not match name format 'G{1} T{2}'.", b.Id, gantryAngle.ToString(), tableAngle.ToString());
                                errorStatus = "2 - Variation";
                                errorSeverity = 1;
                                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                            }
                        }
                    }
                }
                
                if (b.ReferenceImage == null)
                {
                    error = string.Format("Field {0} does not have a DRR.", b.Id);
                    errorStatus = "2 - Variation";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }

                else
                {
                    error = string.Format("Field {0} has a DRR.", b.Id);
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                if (b.IsSetupField == true)
                {
                    if (b.ControlPoints.First().PatientSupportAngle != 0)
                    {
                        error = string.Format("Setup field {0} is not at couch = 0.", b.Id);
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Setup field {0} is at couch = 0.", b.Id);
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    if (b.IsSetupField && (90 - (b.ControlPoints.First().GantryAngle) <= 0.1))
                        ltLatSetupFound = true;
                    if (b.IsSetupField && ((0 - (b.ControlPoints.First().GantryAngle) <= 0.1)))
                        apSetupFound = true;
                    if (b.IsSetupField && (180 - (b.ControlPoints.First().GantryAngle) <= 0.1))
                        paSetupFound = true;
                    if (b.IsSetupField && (270 - (b.ControlPoints.First().GantryAngle) <= 0.1))
                        rtLatSetupFound = true;
                    if (b.IsSetupField && (b.ControlPoints.First().GantryAngle == 0.0) && b.Id == "CBCT")
                        cbctSetupFound = true;
                }
                else    //if field is a treatment field
                {
                    if (b.Wedges.Any())
                    {
                        foreach (var wedge in b.Wedges)
                        {
                            string wedgeTypeString = wedge.GetType().Name;
                            if (wedgeTypeString.Contains("EDW") == true && b.Meterset.Value > 20)
                            {
                                if (b.Meterset.Value > 20)
                                {
                                    error = string.Format("EDW field {0} is more than 20 MU and should be deliverable.", b.Id);
                                    errorStatus = "3 - OK";
                                    errorSeverity = 1;
                                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                                }
                                else
                                {
                                    error = string.Format("EDW field {0} is LESS than 20 MU and should not be deliverable.", b.Id);
                                    errorStatus = "1 - Warning";
                                    errorSeverity = 1;
                                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                                }
                            }
                        }
                    }

                    if (b.MLCPlanType.ToString() == "VMAT")
                    {
                        if (b.ToleranceTableLabel.ToString() == "IMRT")
                        {
                            error = string.Format("Field {0} is VMAT and the tolerance table is IMRT.", b.Id);
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Field {0} is VMAT but the tolerance table is not IMRT.", b.Id);
                            errorStatus = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                    if (b.MLCPlanType.ToString() == "DoseDynamic")
                    {
                        if (b.ToleranceTableLabel.ToString() == "IMRT")
                        {
                            error = string.Format("Field {0} is IMRT and the tolerance table is IMRT.", b.Id);
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Field {0} is IMRT but the tolerance table is not IMRT.", b.Id);
                            error = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                    if (b.MLCPlanType.ToString() == "Static")
                    {
                        if (b.ToleranceTableLabel.ToString() == "T1")
                        {
                            error = string.Format("Field {0} is conformal/3D and the tolerance table is T1.", b.Id);
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Field {0} is conformal/3D but the tolerance table is not T1.", b.Id);
                            error = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                    }
                    if (b.EnergyModeDisplayName.ToString().Contains("E") == true)
                    {
                        if (b.ToleranceTableLabel.ToString().Contains("Electron") == true)
                        {
                            error = string.Format("Field {0} is an electron field and the tolerance table is 'Electron'.", b.Id);
                            errorStatus = "3 - OK";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }
                        else
                        {
                            error = string.Format("Field {0} is an electron field but the tolerance table is not 'Electron'.", b.Id);
                            errorStatus = "1 - Warning";
                            errorSeverity = 1;
                            AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                        }

                    }
                    if (b.Technique.ToString().Contains("STATIC") && b.Meterset.Value > 1000)
                    {
                        error = string.Format("Field {0} is Static, but the MUs are more than 1000.", b.Id);
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                }
            }

            if (rtLatSetupFound == false)
            {
                error = "Setup field Rt Lat not found.";
                errorStatus = "2 - Variation";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = "Setup field Rt Lat found.";
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            if (ltLatSetupFound == false)
            {
                error = "Setup field Lt Lat not found.";
                errorStatus = "2 - Variation";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = "Setup field Lt Lat found.";
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            if (apSetupFound == false)
            {
                error = "Setup field AP not found.";
                errorStatus = "2 - Variation";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = "Setup field AP found.";
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            if (paSetupFound == false)
            {
                error = "Setup field PA not found.";
                errorStatus = "2 - Variation";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = "Setup field PA found.";
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            if (cbctSetupFound == false)
            {
                error = "Setup field CBCT not found.";
                errorStatus = "2 - Variation";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = "Setup field CBCT found.";
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            if (planSetup.TargetVolumeID == planSetup.PrimaryReferencePoint.Id)
            {
                error = string.Format("Target Volume {0} matches Primary Reference Point Id {1}.", planSetup.TargetVolumeID, planSetup.PrimaryReferencePoint.Id);
                errorStatus = "3 - OK";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            else
            {
                error = string.Format("Target Volume {0} does not match Primary Reference Point Id {1}.", planSetup.TargetVolumeID, planSetup.PrimaryReferencePoint.Id);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }

            string planSiteFromPlanName = "";
            if (planSetup.Id.Contains('_'))
            {
                planSiteFromPlanName = planSetup.Id.Split('_')[0];
                if (planSetup.TargetVolumeID.Contains(planSiteFromPlanName))
                {
                    error = string.Format("Planned site {0} matches Target Volume {1}.", planSiteFromPlanName, planSetup.TargetVolumeID);
                    errorStatus = "3 - OK";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
                else
                {
                    error = string.Format("Planned site {0} doesn't match Target Volume {1}.", planSiteFromPlanName, planSetup.TargetVolumeID);
                    errorStatus = "1 - Warning";
                    errorSeverity = 1;
                    AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                }
            }
            else
            {
                error = string.Format("Plan {0} does not have format 'Site_RxDose'.", planSetup.Id);
                errorStatus = "1 - Warning";
                errorSeverity = 1;
                AddNewRow(error, errorStatus, errorSeverity, errorGrid);
            }
            if (planSetup.Id.StartsWith("R ") || planSetup.Id.Contains("_R") || planSetup.Id.Contains("RUL") || planSetup.Id.Contains("RML") || planSetup.Id.Contains("RLL"))
            {
                if (planSetup.TreatmentOrientation.ToString() == "HeadFirstSupine")
                {
                    if (planSetup.Beams.First().IsocenterPosition.x < 0)
                    {
                        error = string.Format("Plan {0} has a right shift of {1} mm and the plan name is labeled Right.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Plan {0} has a right shift of {1} mm but the plan name is not labeled Right.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                }
                if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne" || planSetup.TreatmentOrientation.ToString() == "FeetFirstSupine")
                {
                    if (planSetup.Beams.First().IsocenterPosition.x > 0)
                    {
                        error = string.Format("Plan {0} has a right shift of {1} mm and the plan name is labeled Right.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Plan {0} has a right shift of {1} mm but the plan name is not labeled Right.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                }
            }
            if (planSetup.Id.StartsWith("L ") || planSetup.Id.Contains("_L") || planSetup.Id.Contains("LUL") || planSetup.Id.Contains("LML") || planSetup.Id.Contains("LLL"))
            {
                if (planSetup.TreatmentOrientation.ToString() == "HeadFirstSupine")
                {
                    if (planSetup.Beams.First().IsocenterPosition.x > 0)
                    {
                        error = string.Format("Plan {0} has a left shift of {1} mm and the plan name is labeled Left.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Plan {0} has a left shift of {1} mm but the plan name is not labeled Left.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                }
                if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne" || planSetup.TreatmentOrientation.ToString() == "FeetFirstSupine")
                {
                    if (planSetup.Beams.First().IsocenterPosition.x < 0)
                    {
                        error = string.Format("Plan {0} has a left shift of {1} mm and the plan name is labeled Left.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "3 - OK";
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("Plan {0} has a left shift of {1} mm but the plan name is not labeled Left.", planSetup.Id, planSetup.Beams.First().IsocenterPosition.x.ToString("0.0"));
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                }
            }

            foreach (Course course in planSetup.Course.Patient.Courses)
            {
                if (course.ToString().Contains("QA") == true)
                {
                    if (course.CompletedDateTime == null)
                    {
                        error = string.Format("{0} Course is still active.", course.Id);
                        errorStatus = "1 - Warning";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    else
                    {
                        error = string.Format("{0} Course is inactive.", course.Id);
                        errorStatus = "3 - OK";
                        errorSeverity = 1;
                        AddNewRow(error, errorStatus, errorSeverity, errorGrid);
                    }
                    break;  //QA course found
                }
            }
            return errorGrid;
        }

        double GetDeltaAngle(double a1, double a2)
        {
            double diff = Math.Abs(a1 - a2);
            return diff > 180 ? 360 - diff : diff;
        }

        double CalculateSDT(double deltaAngle, double deltaMU, double maxGantrySpeed, double maxDoseRate)
        {
            double rotationTimeUsingMaxSpeed = deltaAngle / maxGantrySpeed;
            double deliveryTimeUsingMaxDoseRate = deltaMU / maxDoseRate;

            return Math.Max(deliveryTimeUsingMaxDoseRate, rotationTimeUsingMaxSpeed);
        }
    }
}
