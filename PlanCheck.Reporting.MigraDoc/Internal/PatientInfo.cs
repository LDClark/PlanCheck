using System;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace PlanCheck.Reporting.MigraDoc.Internal
{
    internal class PatientInfo
    {
        public static readonly Color Shading = new Color(243, 243, 243);

        public void Add(Section section, ReportPatient patient)
        {
            var table = AddPatientInfoTable(section);

            AddLeftInfo(table.Rows[0].Cells[0], patient);
            AddRightInfo(table.Rows[0].Cells[1], patient);
        }

        private Table AddPatientInfoTable(Section section)
        {
            var table = section.AddTable();
            table.Shading.Color = Shading;

            table.Rows.LeftIndent = 0;

            table.LeftPadding = Size.TableCellPadding;
            table.TopPadding = Size.TableCellPadding;
            table.RightPadding = Size.TableCellPadding;
            table.BottomPadding = Size.TableCellPadding;

            // Use two columns of equal width
            var columnWidth = Size.GetWidth(section) / 2.0;
            table.AddColumn(columnWidth);
            table.AddColumn(columnWidth);

            // Only one row is needed
            table.AddRow();

            return table;
        }

        private void AddLeftInfo(Cell cell, ReportPatient patient)
        {
            // Add patient name and sex symbol
            var p1 = cell.AddParagraph();
            p1.Style = CustomStyles.PatientName;
            p1.AddText($"{patient.LastName}, {patient.FirstName}");
            p1.AddSpace(2);
            AddSexSymbol(p1, patient.Sex);

            // Add patient ID
            var p2 = cell.AddParagraph();
            p2.AddText("ID: ");
            p2.AddFormattedText(patient.Id, TextFormat.Bold);
        }

        private void AddSexSymbol(Paragraph p, Sex sex)
        {
            p.AddImage(new SexSymbol(sex).GetMigraDocFileName());
        }

        private void AddRightInfo(Cell cell, ReportPatient patient)
        {
            var p = cell.AddParagraph();

            // Add birthdate
            p.AddText("Birthdate: ");
            p.AddFormattedText(Format(patient.Birthdate), TextFormat.Bold);

            p.AddLineBreak();

            // Add doctor name
            p.AddText("Doctor: ");
            p.AddFormattedText($" {patient.Doctor.Name}", TextFormat.Bold);
        }

        private string Format(DateTime birthdate)
        {
            return $"{birthdate:d} (age {Age(birthdate)})";
        }

        // See http://stackoverflow.com/a/1404/1383366
        private int Age(DateTime birthdate)
        {
            var today = DateTime.Today;
            int age = today.Year - birthdate.Year;
            return birthdate.AddYears(age) > today ? age - 1 : age;
        }
    }
}
