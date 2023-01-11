using GalaSoft.MvvmLight;

namespace PlanCheck
{
    public class CollisionCheckViewModel : ViewModelBase
    {
        private bool _view;
        public bool View
        {
            get { return _view; }
            set
            {
                _view = value;
                RaisePropertyChanged("View");
            }
        }
        public string FieldID { get; set; }
        public string GantryToTable { get; set; }
        public string GantryToBody { get; set; }
        public string Status { get; set; }
    }
}
