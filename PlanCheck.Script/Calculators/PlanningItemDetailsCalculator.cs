using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class PlanningItemDetailsCalculator
    {
        public ObservableCollection<PlanningItemDetailsViewModel> Calculate(PlanningItemViewModel activePlanningItem, ObservableCollection<PlanningItemViewModel> planningItemComboBoxList, ObservableCollection<PQMViewModel> PqmSummaries, List<CollisionCheckViewModel> CollisionSummaries, List<ErrorViewModel> ErrorGrid)
        {
            var PlanningItemSummaries = new ObservableCollection<PlanningItemDetailsViewModel>();
            var PlanSummary = new ObservableCollection<PlanningItemDetailsViewModel>();
            bool planIsBold = false;
            bool isCCEnabled = false;
            foreach (PlanningItemViewModel planningItem in planningItemComboBoxList)
            {
                string pqmResult = "";
                string ccResult = "";
                string pcResult = "";
                string rpiResult = "";
                if (planningItem.PlanningItemIdWithCourse == activePlanningItem.PlanningItemIdWithCourse)
                {
                    int pqmsPassing = 0;
                    int pqmsTotal = 0;
                    if (PqmSummaries != null)
                    {
                        pqmsTotal = PqmSummaries.Count;
                        foreach (var row in PqmSummaries)
                        {
                            if (row.Met == "Goal" || row.Met == "Variation")
                                pqmsPassing += 1;
                        }
                    }
                    pqmResult = pqmsPassing.ToString() + "/" + pqmsTotal.ToString();
                    int beamCollisionChecksClearing = 0;
                    int beamsTotal = 0;
                    string beamsTotalString = " - ";
                    if (planningItem.PlanningItemObject is PlanSetup)
                    {
                        PlanSetup planSetup = (PlanSetup)planningItem.PlanningItemObject;

                        foreach (Beam b in planSetup.Beams)
                        {
                            if (b.IsSetupField != true)
                                beamsTotal += 1;
                        }
                        beamsTotalString = beamsTotal.ToString();
                    }
                    if (CollisionSummaries != null)
                    {
                        foreach (var row in CollisionSummaries)
                        {
                            if (row.Status == "Clear" || row.Status == " - ")
                                beamCollisionChecksClearing += 1;
                        }
                    }
                    ccResult = beamCollisionChecksClearing.ToString() + "/" + beamsTotal;
                    int errorChecksPassing = 0;
                    int errorChecksTotal = ErrorGrid.Count();
                    foreach (var row in ErrorGrid)
                    {
                        if (row.Status == "3 - OK" || row.Status == "2 - Variation")
                            errorChecksPassing += 1;
                    }
                    pcResult = errorChecksPassing.ToString() + "/" + errorChecksTotal.ToString();
                }

                if (planningItem.PlanningItemObject is PlanSum)
                {
                    PlanSum planSum = (PlanSum)planningItem.PlanningItemObject;
                    int sumOfFractions = 0;
                    double sumOfPlanSetupDoses = 0;
                    isCCEnabled = false;
                    if (planSum == activePlanningItem.PlanningItemObject)
                    {
                        planIsBold = true;
                    }
                    else
                    {
                        planIsBold = false;
                    }

                    foreach (PlanSetup planSetup in planSum.PlanSetups.OrderBy(x => x.CreationDateTime))
                    {
                        string planTarget;
                        sumOfFractions += planSetup.NumberOfFractions.Value;
                        sumOfPlanSetupDoses += planSetup.TotalDose.Dose;
                        if (planSetup.TargetVolumeID != null)
                            planTarget = planSetup.TargetVolumeID;
                        else
                            planTarget = "No target selected";
                    }
                    var PlanSumSummary = new PlanningItemDetailsViewModel
                    {
                        IsBold = planIsBold,
                        CC = isCCEnabled,
                        PlanningItemIdWithCourse = planSum.Course + "/" + planSum.Id,
                        ApprovalStatus = "PlanSum",
                        PlanningItemObject = planningItem.PlanningItemObject,
                        PlanName = planSum.Course + "/" + planSum.Id,
                        PlanCreated = planSum.CreationDateTime.ToString(),
                        PlanFractions = sumOfFractions.ToString(),
                        PlanTotalDose = sumOfPlanSetupDoses.ToString(),
                        PQMResult = pqmResult,
                        CCResult = ccResult,
                        PCResult = pcResult,
                        RPIResult = rpiResult,
                    };
                    PlanningItemSummaries.Add(PlanSumSummary);

                }
                else  //planningitem is plansetup
                {
                    PlanSetup planSetup = (PlanSetup)planningItem.PlanningItemObject;
                    if (planSetup == activePlanningItem.PlanningItemObject)
                    {
                        planIsBold = true;
                        isCCEnabled = true;
                    }
                    else
                    {
                        planIsBold = false;
                        isCCEnabled = false;
                    }
                    var approvalStatus = "";
                    if (planSetup.PlanIntent == "VERIFICATION")
                    {
                        approvalStatus = "VerificationPlan";
                    }
                    else
                        approvalStatus = planSetup.ApprovalStatus.ToString();

                    string planTarget;
                    if (planSetup.TargetVolumeID != null)
                        planTarget = planSetup.TargetVolumeID;
                    else
                        planTarget = "No target selected";
                    var PlanningItemSummary = new PlanningItemDetailsViewModel
                    {
                        IsBold = planIsBold,
                        CC = isCCEnabled,
                        PlanningItemIdWithCourse = planSetup.Course + "/" + planSetup.Id,
                        ApprovalStatus = approvalStatus,
                        PlanName = planSetup.Course + "/" + planSetup.Id,
                        PlanningItemObject = planningItem.PlanningItemObject,
                        PlanCreated = planSetup.CreationDateTime.ToString(),
                        PlanFxDose = planSetup.DosePerFraction.Dose.ToString(),
                        PlanFractions = planSetup.NumberOfFractions.ToString(),
                        PlanTotalDose = planSetup.TotalDose.Dose.ToString(),
                        PlanTarget = planTarget,
                        PQMResult = pqmResult,
                        CCResult = ccResult,
                        PCResult = pcResult,
                        RPIResult = rpiResult,
                    };
                    PlanningItemSummaries.Add(PlanningItemSummary);
                }
            }
            return PlanningItemSummaries;
        }
    }
}
