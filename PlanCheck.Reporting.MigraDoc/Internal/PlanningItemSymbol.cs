using System;
using System.Reflection;

namespace PlanCheck.Reporting.MigraDoc.Internal
{
    internal class PlanningItemSymbol
    {
        private const string PlanSymbolResourceName = "Resources.Plan.png";
        private const string PlanSumSymbolResourceName = "Resources.PlanSum.png";

        private readonly ReportPlanningItem _reportPlanningItem;

        public PlanningItemSymbol(ReportPlanningItem reportPlanningItem)
        {
            _reportPlanningItem = reportPlanningItem;
        }

        public string GetMigraDocFileName()
        {
            return ConvertToMigraDocFileName(LoadResource(PlanningItemSymbolResourceName()));
        }

        // Use special feature in MigraDoc, where instead of using a real file name,
        // it uses a special name that reads the image from memory
        // (see http://www.pdfsharp.net/wiki/MigraDoc_FilelessImages.ashx)
        private string ConvertToMigraDocFileName(byte[] image)
        {
            return $"base64:{Convert.ToBase64String(image)}";
        }

        private byte[] LoadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullName = $"{assembly.GetName().Name}.{name}";
            using (var stream = assembly.GetManifestResourceStream(fullName))
            {
                if (stream == null)
                {
                    throw new ArgumentException($"No resource with name {name}");
                }

                var count = (int)stream.Length;
                var data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }

        private string PlanningItemSymbolResourceName()
        {
            switch (_reportPlanningItem.Type)
            {
                case "Plan": return PlanSymbolResourceName;
                case "PlanSum": return PlanSumSymbolResourceName;

                // Should never reach here
                default: return string.Empty;
            }
        }
    }
}
