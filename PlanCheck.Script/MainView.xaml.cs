using PlanCheck.Helpers;
using PlanCheck.Reporting;
using PlanCheck.Reporting.MigraDoc;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        private MainViewModel _vm;

        public MainView(MainViewModel mainViewModel)
        {
            _vm = mainViewModel;
            InitializeComponent();
            DataContext = _vm;
            pqmDataGrid.Columns[5].Header = _vm.ActivePlanningItem.PlanningItemIdWithCourse;
            SliderBar.Minimum = -135;
            SliderBar.Maximum = 135;
            SliderBar.TickFrequency = 1;
        }

        public void UpdatePqmDataGrid()
        {
            pqmDataGrid.Columns[5].Header = _vm.ActivePlanningItem.PlanningItemIdWithCourse;
            pqmDataGrid.ItemsSource = null;
            pqmDataGrid.ItemsSource = _vm.PqmSummaries;
        }

        private void ConstraintComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = (ConstraintViewModel)ConstraintComboBox.SelectedItem;
            if (_vm.ActiveConstraintPath.ConstraintPath != selection.ConstraintPath)
            {
                _vm.ActiveConstraintPath = (ConstraintViewModel)ConstraintComboBox.SelectedItem;
                var calculator = new PQMSummaryCalculator();
                _vm.GetPQMSummaries(_vm.ActiveConstraintPath, _vm.ActivePlanningItem, _vm.Patient);
                UpdatePqmDataGrid();
                planningItemSummariesDataGrid.ItemsSource = null;
                planningItemSummariesDataGrid.ItemsSource = _vm.PlanningItemSummaries;
            }
        }

        private PlanningItemViewModel GetPlan(object sender)
        {
            var selection = (Button)sender;
            var planningItem = (PlanningItemDetailsViewModel)selection.DataContext;
            return new PlanningItemViewModel(planningItem.PlanningItemObject);
        }

        private PlanningItemViewModel GetPlanFromRadioButton(object sender)
        {
            var selection = (RadioButton)sender;
            var planningItem = (PlanningItemDetailsViewModel)selection.DataContext;
            return new PlanningItemViewModel(planningItem.PlanningItemObject);
        }

        private string GetConstraint(object sender)
        {
            var selection = (ComboBox)sender;
            var constraintPath = (string)selection.DataContext;
            return constraintPath;
        }

        private Structure GetStructure(object sender)
        {
            var selection = (ComboBox)sender;

            var structure = (PQMSummaryViewModel)selection.DataContext;
            return structure.Structure.Structure;
        }

        private void StructureChanged(object sender, EventArgs e)
        {
            var selection = GetStructure(sender);
            UpdatePqmDataGrid();
        }

        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            var plan = GetPlan(sender);
            var tuple = _vm.GetCollisionSummary(plan);
            _vm.CollisionSummaries = tuple.Item1;
            _vm.ModelGroup = tuple.Item2;
            CollisionSummaries.ItemsSource = null;
            CollisionSummaries.ItemsSource = _vm.CollisionSummaries;
            ModelVisual.Content = _vm.ModelGroup;
            _vm.PlanningItemSummaries = _vm.GetPlanningItemSummary(plan, _vm.PlanningItemList);
            planningItemSummariesDataGrid.ItemsSource = null;
            planningItemSummariesDataGrid.ItemsSource = _vm.PlanningItemSummaries;
        }

        private void PrintButtonClicked(object sender, RoutedEventArgs e)
        {
            var reportService = new ReportPdf();
            var reportData = CreateReportData();

            var path = GetTempPdfPath();
            reportService.Export(path, reportData);

            Process.Start(path);
        }
        private Sex GetPatientSex(string sex)
        {
            switch (sex)
            {
                case "Male": return Sex.Male;
                case "Female": return Sex.Female;
                case "Other": return Sex.Other;
                default: throw new ArgumentOutOfRangeException();
            }

        }
        private ReportData CreateReportData()
        {
            var reportPQMs = new ReportPQMs();

            int numPqms = _vm.PqmSummaries.Count();
            ReportPQM[] reportPQMList = new ReportPQM[numPqms];
            var reportPQM = new ReportPQM();
            int i = 0;
            foreach (var pqm in _vm.PqmSummaries)
            {
                reportPQMList[i] = new ReportPQM();
                reportPQMList[i].Achieved = pqm.Achieved;
                reportPQMList[i].DVHObjective = pqm.DVHObjective;
                reportPQMList[i].Goal = pqm.Goal;
                reportPQMList[i].StructureNameWithCode = pqm.Structure.StructureNameWithCode;
                reportPQMList[i].StructVolume = pqm.StructVolume;
                reportPQMList[i].TemplateId = pqm.TemplateId;
                reportPQMList[i].Variation = pqm.Variation;
                reportPQMList[i].Met = pqm.Met;
                i++;
            }
            reportPQMs.PQMs = reportPQMList;

            return new ReportData
            {
                ReportPatient = new ReportPatient
                {
                    Id = _vm.Patient.Id,
                    FirstName = _vm.Patient.FirstName,
                    LastName = _vm.Patient.LastName,
                    Sex = GetPatientSex(_vm.Patient.Sex),
                    Birthdate = (DateTime)_vm.Patient.DateOfBirth,
                    Doctor = new Doctor
                    {
                        Name = _vm.Patient.PrimaryOncologistId
                    }
                },
                ReportPlanningItem = new ReportPlanningItem
                {
                    Id = _vm.ActivePlanningItem.PlanningItemIdWithCourse,
                    Type = _vm.ActivePlanningItem.PlanningItemType,
                    Created = _vm.ActivePlanningItem.Creation
                },
                ReportStructureSet = new ReportStructureSet
                {
                    Id = _vm.StructureSet.Id,
                    Image = new ReportImage
                    {
                        Id = _vm.Image.Id,
                        CreationTime = (DateTime)_vm.Image.CreationDateTime
                    }
                },
                ReportPQMs = reportPQMs,
            };
        }

        private static string GetTempPdfPath()
        {
            return Path.GetTempFileName() + ".pdf";
        }

        private void RadioButtonChecked(object sender, RoutedEventArgs e)
        {
            _vm.ActivePlanningItem = GetPlanFromRadioButton(sender);
            //_vm.ActivePlanningItem = plan;
            _vm.GetPQMSummaries(_vm.ActiveConstraintPath, _vm.ActivePlanningItem, _vm.Patient);
            //pqmDataGrid.Columns[5].Header = plan.PlanningItemIdWithCourse;
            UpdatePqmDataGrid();
            _vm.GetErrors(_vm.ActivePlanningItem);
            errorDataGrid.ItemsSource = null;
            errorDataGrid.ItemsSource = _vm.ErrorGrid;
            _vm.ModelGroup = new Model3DGroup();  //blank?
            _vm.PlanningItemSummaries = _vm.GetPlanningItemSummary(_vm.ActivePlanningItem, _vm.PlanningItemList);
            planningItemSummariesDataGrid.ItemsSource = null;
            planningItemSummariesDataGrid.ItemsSource = _vm.PlanningItemSummaries;
        }

        private void RadioButtonUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            double value = slider.Value;
            var angle = value * Math.PI / 180;
            double x = -3500 * Math.Sin(angle);
            double z = -3500 * Math.Cos(angle);
            perspectiveCamera.LookDirection = new Vector3D(-x, 0, -z);
            perspectiveCamera.Position = new Point3D(x, 0, z);
            ModelVisual.Content = _vm.ModelGroup;
        }
    }
}
