using System.Text.RegularExpressions;
using System;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck.Calculators
{
    class PQMGradientIndex
    {
        public static string GetGradientIndex(StructureSet structureSet, PlanningItemViewModel planningItem, StructureViewModel evalStructure, MatchCollection testMatch, Group evalunit)
        {
            try
            {
                // we have Gradient Index pattern
                DVHData dvh = planningItem.PlanningItemObject.GetDVHCumulativeData(evalStructure.Structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
                if ((dvh.SamplingCoverage < 0.9) || (dvh.Coverage < 0.9))
                {
                    return "Unable to calculate - insufficient dose or sampling coverage";
                }
                Group eval = testMatch[0].Groups["evalpt"];
                Group unit = testMatch[0].Groups["unit"];
                DoseValue prescribedDose;
                double planDoseDouble = 0;
                DoseValue.DoseUnit du = (unit.Value.CompareTo("%") == 0) ? DoseValue.DoseUnit.Percent :
                (unit.Value.CompareTo("cGy") == 0) ? DoseValue.DoseUnit.cGy : DoseValue.DoseUnit.Unknown;
                if (planningItem.PlanningItemObject is PlanSum)
                {
                    PlanSum planSum = (PlanSum)planningItem.PlanningItemObject;
                    foreach (PlanSetup planSetup in planSum.PlanSetups)
                    {
                        planDoseDouble += planSetup.TotalDose.Dose;
                    }

                }
                if (planningItem.PlanningItemObject is PlanSetup)
                {
                    PlanSetup planSetup = (PlanSetup)planningItem.PlanningItemObject;
                    planDoseDouble = planSetup.TotalDose.Dose;
                }
                prescribedDose = new DoseValue(planDoseDouble, DoseValue.DoseUnit.cGy);
                //var body = structureSet.Structures.Where(x => x.Id.Contains("BODY")).First();
                VolumePresentation vpFinal = VolumePresentation.AbsoluteCm3;
                DoseValuePresentation dvpFinal = (evalunit.Value.CompareTo("%") == 0) ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
                DoseValue dv = new DoseValue(double.Parse(eval.Value) / 100 * prescribedDose.Dose, DoseValue.DoseUnit.cGy);
                double bodyWithPrescribedDoseVolume = planningItem.PlanningItemObject.GetVolumeAtDose(evalStructure.Structure, prescribedDose, vpFinal);
                double bodyWithEvalDoseVolume = planningItem.PlanningItemObject.GetVolumeAtDose(evalStructure.Structure, dv, vpFinal);
                var gi = bodyWithEvalDoseVolume / bodyWithPrescribedDoseVolume;
                return string.Format("{0:0.0}", gi);
            }
            catch (NullReferenceException)
            {
                return "Unable to calculate - DVH is not valid";
            }
        }
    }
}
