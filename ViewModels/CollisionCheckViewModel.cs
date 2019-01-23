using System.Windows.Media.Media3D;

namespace PlanCheck
{
    public class CollisionCheckViewModel : ViewModelBase
    {
        public bool View { get; set; }
        public string FieldID { get; set; }
        public string GantryToTableDistance { get; set; }
        public string GantryToBodyDistance { get; set; }
        public string Status { get; set; }
    }
}
