using PlanCheck.Calculators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class PQMSummaryCalculator : ViewModelBase
    {
        public PQMSummaryViewModel[] GetObjectives(ConstraintViewModel constraintVM)
        {
            PQMSummaryViewModel[] m_objectives;
            string WORKBOOK_TEMPLATE_DIR = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string WORKBOOK_RESULT_DIR = System.IO.Path.GetTempPath();

            List<string[]> CSVSheet = new List<string[]>();
            
            string[] header;

            string constraintPath = constraintVM.ConstraintPath;
            

            // make sure the workbook template exists
            if (!System.IO.File.Exists(constraintPath))
            {
                System.Windows.MessageBox.Show(string.Format("The template file '{0}' chosen does not exist.", constraintPath));
                //return;
            }

            CSVSheet = parseCSV(constraintPath);
            //extract header and modify to indicate output values
            header = CSVSheet.First();
            header[2] = "Patient Structure";
            CSVSheet.RemoveAt(0);
            int numFoundObjectives = ReadObjectives(CSVSheet, out m_objectives);
            Console.WriteLine("File " + constraintPath + " loaded.  Objective number is " + numFoundObjectives);
            return m_objectives;
        }

      //  public ObservableCollection<Structure> FindMatchingStructures(ConstraintViewModel constraintVM, StructureSet structureSet)
      //  {
           // PQMSummaryViewModel[] m_objectives = GetObjectives(constraintVM);
           // var evalStructureList = new ObservableCollection<Structure>();
            //int i = 0;
           // foreach (var objective in m_objectives)
           // {
            //    Structure evalStructure = FindStructureFromAlias(structureSet, objective.TemplateId, objective.TemplateAliases, objective.TemplateCodes);
                //objective.Structure = evalStructure;
            //    evalStructureList.Add(evalStructure);
                //i++;
          //  }
         //   return evalStructureList;
       // }

        public PQMSummaryViewModel GetObjectiveProperties(PQMSummaryViewModel objective, PlanningItemViewModel planningItemVM, StructureSet structureSet, StructureViewModel evalStructure)
        {
            objective.ActivePlanningItem = planningItemVM;
            PlanningItem planningItem = planningItemVM.PlanningItemObject;
            if (evalStructure == null)
            {
                objective.Achieved = "Structure not found or empty.";
                objective.isCalculated = false;
                objective.StructureList = StructureSetListViewModel.GetStructureList(structureSet);
                return objective;
            }
            else
            {
                objective.isCalculated = true;
                objective.Structure = evalStructure;
                objective.StructureName = evalStructure.StructureName;
                objective.StructVolume = evalStructure.VolumeValue;
                //objective.StructureNameWithCode = evalStructure.StructureNameWithCode;
                NotifyPropertyChanged("Structure");
                objective.StructureList = StructureSetListViewModel.GetStructureList(structureSet);
                return objective;
            }
        }

        public List<string[]> parseCSV(string path)
        {
            List<string[]> parsedData = new List<string[]>();
            string[] fields;

            try
            {
                var parser = new StreamReader(File.OpenRead(path));

                while (!parser.EndOfStream)
                {
                    fields = parser.ReadLine().Split(',');
                    parsedData.Add(fields);
                }

                parser.Close();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }

            return parsedData;
        }

        private static readonly Regex Whitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return Whitespace.Replace(input, replacement);
        }

        int ReadObjectives(List<string[]> CSVsheet, out PQMSummaryViewModel[] objectives)
        {
            int numFoundObjectives = CSVsheet.Count();

            objectives = new PQMSummaryViewModel[numFoundObjectives];
            int i = 0;
            
            foreach (string[] line in CSVsheet)
            {
                if (line[0] == "")  //A blank line is present
                    break;
                objectives[i] = new PQMSummaryViewModel();
                // Structure ID
                objectives[i].TemplateId = line[0];
                // Structure Code
                string codes = line[1];
                objectives[i].TemplateCodes = (codes.Length > 0) ? ReplaceWhitespace(codes, @"\s+").Split('|') : new string[] { objectives[i].TemplateId };
                // Aliases : extract individual aliases using "|" as separator.  Ignore whitespaces.  If blank, use the ID.
                string aliases = line[2];
                objectives[i].TemplateAliases = (aliases.Length > 0) ? aliases.Split('|') : new string[] { objectives[i].TemplateId };
                // DVH Objective
                objectives[i].DVHObjective = line[4];
                // Evaluator
                objectives[i].Goal = line[5];
                //Variation
                objectives[i].Variation = line[6];
                // Priority
                objectives[i].Priority = line[7];
                // Met (calculate this later, check if meeting - Goal, Variation, Not met)
                objectives[i].Met = "";
                // Achieved (calculate this later)
                objectives[i].Achieved = "";
                i++;
            }
            return numFoundObjectives;
        }

        public Structure FindStructureFromAlias(StructureSet ss, string ID, string[] aliases, string[] codes)
        {
            // search through the list of alias ids until we find an alias that matches an existing structure.
            Structure oar = null;
            string actualStructId = "";
            oar = (from s in ss.Structures
                   where s.Id.ToUpper().CompareTo(ID.ToUpper()) == 0
                   select s).FirstOrDefault();
            if (oar == null)
            {
                foreach (string alias in aliases)
                {
                    oar = (from s in ss.Structures
                           where s.Id.ToUpper().CompareTo(alias.ToUpper()) == 0
                           select s).FirstOrDefault();
                    if (oar != null && oar.IsEmpty != true)
                    {
                        actualStructId = oar.Id;
                        //return oar;
                        break;
                    }
                    else
                    {
                        foreach (string code in codes)  //try to find structure by code
                        {
                            oar = (from s in ss.Structures
                                   where s.StructureCodeInfos.FirstOrDefault().Code != null && s.StructureCodeInfos.FirstOrDefault().Code.ToString().CompareTo(code) == 0
                                   select s).LastOrDefault();
                            if (oar != null)
                            {
                                actualStructId = oar.Id;
                                //return oar;
                                break;
                            }
                        }
                    }
                }
            }

            if ((oar != null) && (oar.IsEmpty))
            {
                oar = null;
            }
            return oar;
        }

        void ConvertUnitToGy(ref string expression)
        {
            if (string.IsNullOrEmpty(expression)) return;
            expression = expression.Replace("cGy", "Gy");
        }

        void ConvertUnitTocGy(ref string expression)
        {
            if (string.IsNullOrEmpty(expression)) return;
            expression = expression.Replace("Gy", "cGy");
        }

        void ConvertValueToGy(ref string expression)
        {
            var resultString = Regex.Match(expression, @"\d+\p{P}\d+|\d+").Value;
            double newValue = double.NaN;
            if (double.TryParse(resultString, out newValue))
            {
                newValue = newValue / 100.0;
                expression = expression.Replace(resultString, newValue.ToString());
            }
        }

        void ConvertValueTocGy(ref string expression)
        {
            var resultString = Regex.Match(expression, @"\d+\p{P}\d+|\d+").Value;
            double newValue = double.NaN;
            if (double.TryParse(resultString, out newValue))
            {
                newValue = newValue * 100.0;
                expression = expression.Replace(resultString, newValue.ToString());
            }
        }
        public string CalculateMetric(StructureSet structureSet, StructureViewModel evalStructureVM, PlanningItemViewModel planningItem, string DVHObjective)
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

            Structure evalStructure = evalStructureVM.Structure;
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
                                        return PQMGradientIndex.GetGradientIndex(structureSet, planningItem, evalStructure, testMatch, evalunit);
                                    }
                                }
                                else
                                {
                                    // we have Conformation Number pattern
                                    return PQMConformationNumber.GetConformationNumber(structureSet, planningItem, evalStructure, testMatch, evalunit);
                                }

                            }
                            else
                            {
                                // we have Covered Dose at Volume pattern
                                return PQMCoveredDoseAtVolume.GetCoveredDoseAtVolume(structureSet, planningItem, evalStructure, testMatch, evalunit);
                            }
                        }
                        else
                        {
                            // we have Covered Volume at Dose pattern
                            return PQMCoveredVolumeAtDose.GetCoveredVolumeAtDose(structureSet, planningItem, evalStructure, testMatch, evalunit);
                        }
                    }
                    else
                    {
                        // we have Dose at Volume pattern
                        return PQMDoseAtVolume.GetDoseAtVolume(structureSet, planningItem, evalStructure, testMatch, evalunit);
                    }
                }
                else
                {
                    // we have Volume at Dose pattern
                    return PQMVolumeAtDose.GetVolumeAtDose(structureSet, planningItem, evalStructure, testMatch, evalunit);
                }
            }
            else
            {
                // we have Min, Max, Mean, or Volume
                return PQMMinMaxMean.GetMinMaxMean(structureSet, planningItem, evalStructure, testMatch, evalunit, type);
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
