using System;
using VMS.TPS.Common.Model.API;
using System.Text.RegularExpressions;
using PlanCheck.Calculators;

namespace PlanCheck
{
    // This class works directly with ESAPI objects, but it will be wrapped by EsapiService,
    // which doesn't expose ESAPI objects in order to isolate the script from ESAPI
    public class MetricCalculator
    {
        public string CalculateMetric(StructureViewModel evalStructure, PlanningItemViewModel planningItem, string DVHObjective)
        {

            //start with a general regex that pulls out the metric type and the @ (evalunit) part.
            string pattern = @"^(?<type>[^\[\]]+)(\[(?<evalunit>[^\[\]]+)\])$";
            string minmaxmean_Pattern = @"^(M(in|ax|ean)|Volume)$";//check for Max or Min or Mean or Volume
            string d_at_v_pattern = @"^D(?<evalpt>\d+\p{P}\d+|\d+)(?<unit>(%|cc))$"; // matches D95%, D2cc
            string dc_at_v_pattern = @"^DC(?<evalpt>\d+)(?<unit>(%|cc))$"; // matches DC95%, DC700cc
            string v_at_d_pattern = @"^V(?<evalpt>\d+\p{P}\d+|\d+)(?<unit>(%|Gy|cGy))$"; // matches V98%, V40Gy
            string cv_at_d_pattern = @"^CV(?<evalpt>\d+)(?<unit>(%|Gy|cGy))$"; // matches CV98%, CV40Gy
                                                                               // Max[Gy] D95%[%] V98%[%] CV98%[%] D2cc[Gy] V40Gy[%]
            string cn_pattern = @"^CN(?<evalpt>\d+\p{P}\d+|\d+)(?<unit>(%|Gy|cGy))$"; //matches CN50%   
            string gi_pattern = @"^GI(?<evalpt>\d+\p{P}\d+|\d+)(?<unit>(%|Gy|cGy))$"; //matches GI50%   

            //Structure evalStructure = ;
            if (evalStructure == null)
                return "Structure not found";

            //start with a general regex that pulls out the metric type and the [evalunit] part.
            var matches = Regex.Matches(DVHObjective, pattern);

            if (matches.Count != 1)
            {
                return string.Format("DVH Objective expression \"{0}\" is not a recognized expression type.", DVHObjective);
            }
            Match m = matches[0];
            Group type = m.Groups["type"];
            Group evalunit = m.Groups["evalunit"];
            Console.WriteLine("expression {0} => type = {1}, unit = {2}", DVHObjective, type.Value, evalunit.Value);

            // further decompose <type>
            var testMatch = Regex.Matches(type.Value, minmaxmean_Pattern);
            if (testMatch.Count != 1)
            {
                testMatch = Regex.Matches(type.Value, v_at_d_pattern);
                if (testMatch.Count != 1)
                {
                    testMatch = Regex.Matches(type.Value, d_at_v_pattern);
                    if (testMatch.Count != 1)
                    {
                        testMatch = Regex.Matches(type.Value, cv_at_d_pattern);
                        if (testMatch.Count != 1)
                        {
                            testMatch = Regex.Matches(type.Value, dc_at_v_pattern);
                            if (testMatch.Count != 1)
                            {
                                testMatch = Regex.Matches(type.Value, cn_pattern);
                                if (testMatch.Count != 1)
                                {
                                    testMatch = Regex.Matches(type.Value, gi_pattern);
                                    if (testMatch.Count != 1)
                                    {

                                        return string.Format("DVH Objective expression \"{0}\" is not a recognized expression type.", DVHObjective);
                                    }
                                    else
                                    {
                                        // we have Gradient Index pattern
                                        return PQMGradientIndex.GetGradientIndex(planningItem, evalStructure, testMatch, evalunit);
                                    }
                                }
                                else
                                {
                                    // we have Conformation Number pattern
                                    return PQMConformationNumber.GetConformationNumber(planningItem, evalStructure, testMatch, evalunit);
                                }

                            }
                            else
                            {
                                // we have Covered Dose at Volume pattern
                                return PQMCoveredDoseAtVolume.GetCoveredDoseAtVolume(planningItem, evalStructure, testMatch, evalunit);
                            }
                        }
                        else
                        {
                            // we have Covered Volume at Dose pattern
                            return PQMCoveredVolumeAtDose.GetCoveredVolumeAtDose(planningItem, evalStructure, testMatch, evalunit);
                        }
                    }
                    else
                    {
                        // we have Dose at Volume pattern
                        return PQMDoseAtVolume.GetDoseAtVolume(planningItem, evalStructure, testMatch, evalunit);
                    }
                }
                else
                {
                    // we have Volume at Dose pattern
                    return PQMVolumeAtDose.GetVolumeAtDose(planningItem, evalStructure, testMatch, evalunit);
                }
            }
            else
            {
                // we have Min, Max, Mean, or Volume
                return PQMMinMaxMean.GetMinMaxMean(planningItem, evalStructure, testMatch, evalunit, type);
            }
        }

        // further decompose <ateval>
        //look at the evaluator and compare to goal or variation
        public string EvaluateMetric(string achieved, string goal, string variation)
        {
            string met = "";
            string evalpattern = @"^(?<type><|<=|=|>=|>)(?<goal>\d+\p{P}\d+|\d+)$";
            if (!String.IsNullOrEmpty(goal))
            {
                var matches = Regex.Matches(goal, evalpattern);
                if (matches.Count != 1)
                {
                    System.Windows.MessageBox.Show("Eval pattern not recognized");
                    return string.Format("Evaluator expression \"{0}\" is not a recognized expression type.", goal);
                }
                Match m = matches[0];
                Group goalGroup = m.Groups["goal"];
                Group evaltype = m.Groups["type"];


                if (String.IsNullOrEmpty(Regex.Match(achieved, @"\d+\p{P}\d+|\d+").Value))
                {
                    met = "Not evaluated";
                }
                else
                {
                    double evalvalue = Double.Parse(Regex.Match(achieved, @"\d+\p{P}\d+|\d+").Value);
                    if (evaltype.Value.CompareTo("<") == 0)
                    {
                        if ((evalvalue - Double.Parse(goalGroup.ToString())) < 0)
                        {
                            met = "Goal";
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(variation))
                            {
                                met = "Not met";
                            }
                            else
                            {
                                if ((evalvalue - Double.Parse(variation)) < 0)
                                {
                                    met = "Variation";
                                }
                                else
                                {
                                    met = "Not met";
                                }
                            }
                        }
                    }
                    else if (evaltype.Value.CompareTo("<=") == 0)
                    {
                        //MessageBox.Show("evaluating <= " + evaltype.ToString());
                        if ((evalvalue - Double.Parse(goalGroup.ToString())) <= 0)
                        {
                            met = "Goal";
                        }
                        else
                        {
                            //MessageBox.Show("Evaluating variation");
                            if (String.IsNullOrEmpty(variation))
                            {
                                //MessageBox.Show(String.Format("Empty variation condition Achieved: {0} Variation: {1}", objective.Achieved.ToString(), objective.Variation.ToString()));
                                met = "Not met";
                            }
                            else
                            {
                                //MessageBox.Show(String.Format("Non Empty variation condition Achieved: {0} Variation: {1}", objective.Achieved.ToString(), objective.Variation.ToString()));
                                if ((evalvalue - Double.Parse(variation)) <= 0)
                                {
                                    met = "Variation";
                                }
                                else
                                {
                                    met = "Not met";
                                }
                            }
                        }
                    }
                    else if (evaltype.Value.CompareTo("=") == 0)
                    {
                        if ((evalvalue - Double.Parse(goalGroup.ToString())) == 0)
                        {
                            met = "Goal";
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(variation))
                            {
                                met = "Not met";
                            }
                            else
                            {
                                if ((evalvalue - Double.Parse(variation)) == 0)
                                {
                                    met = "Variation";
                                }
                                else
                                {
                                    met = "Not met";
                                }
                            }
                        }
                    }
                    else if (evaltype.Value.CompareTo(">=") == 0)
                    {
                        if ((evalvalue - Double.Parse(goalGroup.ToString())) >= 0)
                        {
                            met = "Goal";
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(variation))
                            {
                                met = "Not met";
                            }
                            else
                            {
                                if ((evalvalue - Double.Parse(variation)) >= 0)
                                {
                                    met = "Variation";
                                }
                                else
                                {
                                    met = "Not met";
                                }
                            }
                        }
                    }
                    else if (evaltype.Value.CompareTo(">") == 0)
                    {
                        if ((evalvalue - Double.Parse(goalGroup.ToString())) > 0)
                        {
                            met = "Goal";
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(variation))
                            {
                                met = "Not met";
                            }
                            else
                            {
                                if ((evalvalue - Double.Parse(variation)) > 0)
                                {
                                    met = "Variation";
                                }
                                else
                                {
                                    met = "Not met";
                                }
                            }
                        }
                    }
                }
            }
            return met;
        }
    }
}