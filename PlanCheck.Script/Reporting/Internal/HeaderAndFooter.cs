using System;
using MigraDoc.DocumentObjectModel;

namespace PlanCheck.Reporting.MigraDoc.Internal
{
    internal class HeaderAndFooter
    {
        public void Add(Section section, ReportData data)
        {
            AddHeader(section, data.ReportPatient);
            AddFooter(section);
        }

        private void AddHeader(Section section, ReportPatient patient)
        {
            var header = section.Headers.Primary.AddParagraph();
            header.Format.AddTabStop(Size.GetWidth(section), TabAlignment.Right);

            header.AddText($"{patient.LastName}, {patient.FirstName} (ID: {patient.Id})");
            header.AddTab();
            header.AddText($"Generated {DateTime.Now:g}");
        }

        private void AddFooter(Section section)
        {
            var footer = section.Footers.Primary.AddParagraph();
            footer.Format.AddTabStop(Size.GetWidth(section), TabAlignment.Right);

            footer.AddText("PQM Report");
            footer.AddTab();
            footer.AddText("Page ");
            footer.AddPageField();
            footer.AddText(" of ");
            footer.AddNumPagesField();
        }
    }
}
