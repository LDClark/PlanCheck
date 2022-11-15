using System;

namespace PlanCheck.Reporting
{
    public class ReportPatient
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Sex Sex { get; set; }
        public DateTime Birthdate { get; set; }
        public Doctor Doctor { get; set; }
    }
}
