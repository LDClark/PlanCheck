using MigraDoc.DocumentObjectModel;

namespace PlanCheck.Reporting.MigraDoc.Internal
{
    internal class Size
    {
        // Top and bottom margins are larger to account for the header and footer
        public static readonly Unit TopBottomPageMargin = "0.75 in";
        public static readonly Unit LeftRightPageMargin = "0.50 in";

        public static readonly Unit HeaderFooterMargin = "0.25 in";

        public static readonly Unit TableCellPadding = "0.07 in";

        public static Unit GetWidth(Section section)
        {
            Unit pageWidth;
            Unit _;
            PageSetup.GetPageSize(section.PageSetup.PageFormat, out pageWidth, out _);
            return pageWidth - section.PageSetup.LeftMargin - section.PageSetup.RightMargin;
        }
    }
}
