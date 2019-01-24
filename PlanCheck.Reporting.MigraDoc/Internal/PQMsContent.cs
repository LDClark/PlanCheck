using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace PlanCheck.Reporting.MigraDoc.Internal
{
    internal class PQMsContent
    {
        public void Add(Section section, ReportStructureSet structureSet, ReportPlanningItem reportPlanningItem, ReportPQMs reportPQMs)
        {
            AddHeading(section, structureSet, reportPlanningItem);
            AddPQMs(section, reportPQMs);
        }

        private void AddHeading(Section section, ReportStructureSet structureSet, ReportPlanningItem reportPlanningItem)
        {
            Paragraph p = section.AddParagraph();
            p.Style = StyleNames.Heading1;
            AddPlanningItemSymbol(p, reportPlanningItem);
            p.AddText(reportPlanningItem.Type + " : " + reportPlanningItem.Id);
            section.AddParagraph($"created {reportPlanningItem.Created:g}");
            section.AddParagraph($"Image {structureSet.Image.Id} " +
                                 $"taken {structureSet.Image.CreationTime:g}");
        }

        private void AddPlanningItemSymbol(Paragraph p, ReportPlanningItem reportPlanningItem)
        {
            p.AddImage(new PlanningItemSymbol(reportPlanningItem).GetMigraDocFileName());
        }

        private void AddPQMs(Section section, ReportPQMs PQMs)
        {
            AddTableTitle(section, "PQMs");
            AddPQMTable(section, PQMs);
        }

        private void AddTableTitle(Section section, string title)
        {
            var p = section.AddParagraph(title, StyleNames.Heading2);
            p.Format.KeepWithNext = true;
        }

        private void AddPQMTable(Section section, ReportPQMs PQMs)
        {
            var table = section.AddTable();

            FormatTable(table);
            AddColumnsAndHeaders(table);
            AddPQMRows(table, PQMs);

            AddLastRowBorder(table);
            AlternateRowShading(table);
        }

        private static void FormatTable(Table table)
        {
            table.LeftPadding = 0;
            table.TopPadding = Size.TableCellPadding;
            table.RightPadding = 0;
            table.BottomPadding = Size.TableCellPadding;
            table.Format.LeftIndent = Size.TableCellPadding;
            table.Format.RightIndent = Size.TableCellPadding;
        }

        private void AddColumnsAndHeaders(Table table)
        {
            var width = Size.GetWidth(table.Section);
            table.AddColumn(width * 0.2);
            table.AddColumn(width * 0.2);
            table.AddColumn(width * 0.1);
            table.AddColumn(width * 0.15);
            table.AddColumn(width * 0.1);
            table.AddColumn(width * 0.15);
            table.AddColumn(width * 0.1);

            var headerRow = table.AddRow();
            headerRow.Borders.Bottom.Width = 1;

            AddHeader(headerRow.Cells[0], "Template ID");
            AddHeader(headerRow.Cells[1], "Structure");
            AddHeader(headerRow.Cells[2], "Vol [cc]");
            AddHeader(headerRow.Cells[3], "DVH Objective");
            AddHeader(headerRow.Cells[4], "Goal");
            AddHeader(headerRow.Cells[5], "Achieved");
            AddHeader(headerRow.Cells[6], "Met");
        }

        private void AddHeader(Cell cell, string header)
        {
            var p = cell.AddParagraph(header);
            p.Style = CustomStyles.ColumnHeader;
        }

        private void AddPQMRows(Table table, ReportPQMs PQMs)
        {
            foreach (var pqm in PQMs.PQMs)
            {
                var row = table.AddRow();
                row.Format.Font.Size = 10;
                row.VerticalAlignment = VerticalAlignment.Center;

                row.Cells[0].AddParagraph(pqm.TemplateId);
                row.Cells[1].AddParagraph(pqm.StructureNameWithCode);
                row.Cells[2].AddParagraph(pqm.StructVolume);
                row.Cells[3].AddParagraph(pqm.DVHObjective);
                row.Cells[4].AddParagraph(pqm.Goal);
                row.Cells[5].AddParagraph(pqm.Achieved);
                row.Cells[6].AddParagraph(pqm.Met);
            }
        }

        private void AddLastRowBorder(Table table)
        {
            var lastRow = table.Rows[table.Rows.Count - 1];
            lastRow.Borders.Bottom.Width = 2;
        }

        private void AlternateRowShading(Table table)
        {
            // Start at i = 1 to skip column headers
            for (var i = 1; i < table.Rows.Count; i++)
            {
                if (i % 2 == 0)  // Even rows
                {
                    table.Rows[i].Shading.Color = Color.FromRgb(216, 216, 216);
                }
            }
        }
    }
}
