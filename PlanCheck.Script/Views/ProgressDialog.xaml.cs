using System.Windows;

namespace PlanCheck
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
            ProgressBar.IsIndeterminate = true;
        }

        public string Message
        {
            get => MessageTextBlock.Text;
            set => MessageTextBlock.Text = value;
        }

        public int MaxProgress
        {
            get => (int)ProgressBar.Maximum;
            set
            {
                ProgressBar.Maximum = value;
                ProgressBar.IsIndeterminate = false;
            }
        }

        public void IncrementProgress()
        {
            ProgressBar.Value += 1;
        }
    }
}
