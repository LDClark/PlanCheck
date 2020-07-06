using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCheck.Calculators
{
    class PQMMinMaxMean
    {
        public static string GetMinMaxMean(StructureSet structureSet, PlanningItemViewModel planningItem, Structure evalStructure, MatchCollection testMatch, Group evalunit, Group type)
        {
            try
            {
                if (type.Value.CompareTo("Volume") == 0)
                {
                    return string.Format("{0:0.00} {1}", evalStructure.Volume, evalunit.Value);
                }
                else
                {
                    double planSumRxDose = 0;
                    DVHData dvh;
                    DoseValuePresentation dvp = (evalunit.Value.CompareTo("%") == 0) ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
                    if (dvp == DoseValuePresentation.Relative && planningItem.PlanningItemObject is PlanSum)
                    {
                        PlanSum planSum = (PlanSum)planningItem.PlanningItemObject;
                        foreach (PlanSetup planSetup in planSum.PlanSetups)
                        {
                            double planSetupRxDose = planSetup.TotalDose.Dose;
                            planSumRxDose += planSetupRxDose;
                        }
                        dvh = planningItem.PlanningItemObject.GetDVHCumulativeData(evalStructure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
                    }
                    else
                        dvh = planningItem.PlanningItemObject.GetDVHCumulativeData(evalStructure, dvp, VolumePresentation.Relative, 0.1);
                    if (type.Value.CompareTo("Max") == 0)
                    {
                        //checking dose output unit and adapting to template
                        //Gy to cGy
                        if ((evalunit.Value.CompareTo("Gy") == 0) && (dvh.MaxDose.Unit.CompareTo(DoseValue.DoseUnit.cGy) == 0))
                        {
                            return new DoseValue(dvh.MaxDose.Dose / 100, DoseValue.DoseUnit.Gy).ToString();
                        }
                        //Gy to Gy or % to %
                        else
                        {
                            if (dvp == DoseValuePresentation.Relative && planningItem.PlanningItemObject is PlanSum)
                            {
                                double maxDoseDouble = double.Parse(dvh.MaxDose.ValueAsString);
                                //double 
                                return (maxDoseDouble / planSumRxDose * 100).ToString("0.0") + " " + evalunit.Value;
                            }
                            else
                                return dvh.MaxDose.ToString();
                        }
                    }
                    else if (type.Value.CompareTo("Min") == 0)
                    {
                        //checking dose output unit and adapting to template
                        //Gy to cGy
                        if ((evalunit.Value.CompareTo("Gy") == 0) && (dvh.MinDose.Unit.CompareTo(DoseValue.DoseUnit.cGy) == 0))
                        {
                            return new DoseValue(dvh.MinDose.Dose / 100, DoseValue.DoseUnit.Gy).ToString();
                        }
                        //Gy to Gy or % to %
                        else
                        {
                            return dvh.MinDose.ToString();
                        }
                    }
                    else
                    {
                        //checking dose output unit and adapting to template
                        //Gy to cGy
                        if ((evalunit.Value.CompareTo("Gy") == 0) && (dvh.MeanDose.Unit.CompareTo(DoseValue.DoseUnit.cGy) == 0))
                        {
                            return new DoseValue(dvh.MeanDose.Dose / 100, DoseValue.DoseUnit.Gy).ToString();
                        }
                        //Gy to Gy or % to %
                        else
                        {
                            return dvh.MeanDose.ToString();
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
                return "Unable to calculate - DVH is not valid";
            }
        }
    }
}
