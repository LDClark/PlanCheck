using System;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck.Calculators
{
    class PQMDoseAtVolume
    {
        public static string GetDoseAtVolume(PlanningItemViewModel planningItem, StructureViewModel evalStructure, MatchCollection testMatch, Group evalunit)
        {      
            try
            {
                DVHData dvh = planningItem.Object.GetDVHCumulativeData(evalStructure.Object, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);

                //check for sufficient sampling and dose coverage
                if ((dvh.SamplingCoverage < 0.9) || (dvh.Coverage < 0.9))
                    return "Unable to calculate - insufficient dose or sampling coverage";

                Group eval = testMatch[0].Groups["evalpt"];
                Group unit = testMatch[0].Groups["unit"];
                DoseValue.DoseUnit du = (unit.Value.CompareTo("%") == 0) ? DoseValue.DoseUnit.Percent :
                        (unit.Value.CompareTo("cGy") == 0) ? DoseValue.DoseUnit.cGy : DoseValue.DoseUnit.Unknown;
                VolumePresentation vp = (unit.Value.CompareTo("%") == 0) ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                DoseValue dv = new DoseValue(double.Parse(eval.Value), du);
                double volume = double.Parse(eval.Value);
                VolumePresentation vpFinal = (evalunit.Value.CompareTo("%") == 0) ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                DoseValuePresentation dvpFinal = (evalunit.Value.CompareTo("%") == 0) ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
                DoseValue dvAchieved = planningItem.Object.GetDoseAtVolume(evalStructure.Object, volume, vp, dvpFinal);

                //checking dose output unit and adapting to template evalunits
                if (dvAchieved.UnitAsString.CompareTo(evalunit.Value.ToString()) != 0)
                {
                    if ((evalunit.Value.CompareTo("cGy") == 0) && (dvAchieved.Unit.CompareTo(DoseValue.DoseUnit.Gy) == 0)) //switch units to cGy
                        dvAchieved = new DoseValue(dvAchieved.Dose * 100, DoseValue.DoseUnit.cGy);
                    else if ((evalunit.Value.CompareTo("Gy") == 0) && (dvAchieved.Unit.CompareTo(DoseValue.DoseUnit.cGy) == 0)) //switch units to Gy
                        dvAchieved = new DoseValue(dvAchieved.Dose / 100, DoseValue.DoseUnit.Gy);
                    else
                        return "Unable to calculate";
                }

                return dvAchieved.ToString();
            }
            catch (NullReferenceException)
            {
                return "Unable to calculate - DVH is not valid";
            }
            catch (ApplicationException)
            {
                return "Unable to calculate - constraint is not valid";
            }
        }
    }
}
