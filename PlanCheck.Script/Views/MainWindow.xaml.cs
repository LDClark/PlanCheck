using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media.Media3D;

namespace PlanCheck
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Force Interactivity assembly to be included in build files
            Interaction.GetBehaviors(this);

            InitializeComponent();
        }
    }
}
