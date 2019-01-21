using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck.Calculators
{
    class PQMGradientIndex
    {
        public static string GetGradientIndex(StructureSet structureSet, PlanningItem planningItem, Structure evalStructure, MatchCollection testMatch, Group evalunit)
        {
            // we have Gradient Index pattern
            DVHData dvh = planningItem.GetDVHCumulativeData(evalStructure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
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
            if (planningItem is PlanSum)
            {
                PlanSum planSum = (PlanSum)planningItem;
                foreach (PlanSetup planSetup in planSum.PlanSetups)
                {
                    planDoseDouble += planSetup.TotalDose.Dose;
                }

            }
            if (planningItem is PlanSetup)
            {
                PlanSetup planSetup = (PlanSetup)planningItem;
                planDoseDouble = planSetup.TotalDose.Dose;
            }
            prescribedDose = new DoseValue(planDoseDouble, DoseValue.DoseUnit.cGy);
            //var body = structureSet.Structures.Where(x => x.Id.Contains("BODY")).First();
            VolumePresentation vpFinal = VolumePresentation.AbsoluteCm3;
            DoseValuePresentation dvpFinal = (evalunit.Value.CompareTo("%") == 0) ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
            DoseValue dv = new DoseValue(double.Parse(eval.Value) / 100 * prescribedDose.Dose, DoseValue.DoseUnit.cGy);
            double bodyWithPrescribedDoseVolume = planningItem.GetVolumeAtDose(evalStructure, prescribedDose, vpFinal);
            double bodyWithEvalDoseVolume = planningItem.GetVolumeAtDose(evalStructure, dv, vpFinal);
            var gi = bodyWithEvalDoseVolume / bodyWithPrescribedDoseVolume;
            return string.Format("{0:0.0}", gi);
        }
    }
}
