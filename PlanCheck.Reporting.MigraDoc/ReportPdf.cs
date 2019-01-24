using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PlanCheck.Reporting.MigraDoc.Internal;

namespace PlanCheck.Reporting.MigraDoc
{
    public class ReportPdf : IReport
    {
        public void Export(string path, ReportData data)
        {
            ExportPdf(path, CreateReport(data));
        }

        private void ExportPdf(string path, Document report)
        {
            var pdfRenderer = new PdfDocumentRenderer();
            pdfRenderer.Document = report;
            pdfRenderer.RenderDocument();
            pdfRenderer.PdfDocument.Save(path);
        }

        private Document CreateReport(ReportData data)
        {
            var doc = new Document();
            CustomStyles.Define(doc);
            doc.Add(CreateMainSection(data));
            return doc;
        }

        private Section CreateMainSection(ReportData data)
        {
            var section = new Section();
            SetUpPage(section);
            AddHeaderAndFooter(section, data);
            AddContents(section, data);
            return section;
        }

        private void SetUpPage(Section section)
        {
            section.PageSetup.PageFormat = PageFormat.Letter;

            section.PageSetup.LeftMargin = Size.LeftRightPageMargin;
            section.PageSetup.TopMargin = Size.TopBottomPageMargin;
            section.PageSetup.RightMargin = Size.LeftRightPageMargin;
            section.PageSetup.BottomMargin = Size.TopBottomPageMargin;

            section.PageSetup.HeaderDistance = Size.HeaderFooterMargin;
            section.PageSetup.FooterDistance = Size.HeaderFooterMargin;
        }

        private void AddHeaderAndFooter(Section section, ReportData data)
        {
            new HeaderAndFooter().Add(section, data);
        }

        private void AddContents(Section section, ReportData data)
        {
            AddPatientInfo(section, data.ReportPatient);
            AddPQMs(section, data.ReportStructureSet, data.ReportPlanningItem, data.ReportPQMs);
        }

        private void AddPatientInfo(Section section, ReportPatient patient)
        {
            new PatientInfo().Add(section, patient);
        }

        private void AddPQMs(Section section, ReportStructureSet structureSet, ReportPlanningItem reportPlanningItem, ReportPQMs reportPQMs)
        {
            new PQMsContent().Add(section, structureSet, reportPlanningItem, reportPQMs);
        }
    }
}
