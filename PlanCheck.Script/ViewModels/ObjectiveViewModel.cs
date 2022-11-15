using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class ObjectiveViewModel
    {
        public static PQMViewModel[] GetObjectives(ConstraintViewModel constraint)
        {
            List<string[]> CSVSheet = new List<string[]>();

            CSVSheet = parseCSV(constraint.ConstraintPath);
            //extract header and modify to indicate output values
            PQMViewModel[] objectives = new PQMViewModel[CSVSheet.Count()];
            int i = 0;

            foreach (string[] line in CSVSheet)
            {
                if (line[0] == "")  //A blank line is present
                    continue;
                if (line[0] == "Structure IDs")
                    continue;
                objectives[i] = new PQMViewModel();
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
                objectives[i].Variation = line[6];
                objectives[i].Priority = line[7];
                objectives[i].Achieved = "";  //find this later
                objectives[i].Met = "";  //find this later
                i++;
            }
            return objectives;
        }

        public static List<string[]> parseCSV(string path)
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
    }
}