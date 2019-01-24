using PlanCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class MainViewModel : ViewModelBase
    {
        public Patient Patient { get; set; }
        public Image Image { get; set; }
        public StructureSet StructureSet { get; set; }
        public string Title { get; set; }
        public ConstraintViewModel ActiveConstraintPath { get; set; }
        public PlanningItemViewModel ActivePlanningItem { get; set; }
        public List<ErrorViewModel> ErrorGrid { get; set; }
        public ObservableCollection<PQMSummaryViewModel> PqmSummaries { get; set; }

        public ObservableCollection<StructureViewModel> _FoundStructureList;
        public ObservableCollection<StructureViewModel> FoundStructureList
        {
            get { return _FoundStructureList; }
            set
            {
                _FoundStructureList = value;
                NotifyPropertyChanged("FoundStructureList");
            }
        }
        PQMSummaryViewModel[] Objectives { get; set; }
        public ObservableCollection<PlanningItemViewModel> PlanningItemList { get; set; }
        public ObservableCollection<ConstraintViewModel> ConstraintComboBoxList { get; set; }
        public ObservableCollection<PlanningItemDetailsViewModel> PlanningItemSummaries { get; set; }
        public List<CollisionCheckViewModel> CollisionSummaries { get; set; }
        public double SliderValue { get; set; }
        public Model3DGroup ModelGroup { get; set; }
        public Point3D isoctr { get; set; }
        public Point3D cameraPosition { get; set; }
        public Vector3D upDir { get; set; }
        public Vector3D lookDir { get; set; }

        public MainViewModel(User user, Patient patient, string scriptVersion, string constraintPath, ObservableCollection<PlanningItemViewModel> planningItemList, PlanningItemViewModel planningItem)
        {
            ActivePlanningItem = planningItem;
            Patient = patient;
            Image = ActivePlanningItem.PlanningItemImage;
            StructureSet = ActivePlanningItem.PlanningItemStructureSet;
            DirectoryInfo constraintDir = new DirectoryInfo(Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "ConstraintTemplates"));
            ActiveConstraintPath = new ConstraintViewModel(constraintPath);
            PlanningItemList = planningItemList;
            ConstraintComboBoxList = ConstraintListViewModel.GetConstraintList(constraintDir.ToString());
            PqmSummaries = GetPQMSummaries(ActiveConstraintPath, ActivePlanningItem, Patient);
            ErrorGrid = GetErrors(ActivePlanningItem);
            Title = GetTitle(patient, scriptVersion);
            ModelGroup = new Model3DGroup();
            SliderValue = 0;
            upDir = new Vector3D(0, -1, 0);
            lookDir = new Vector3D(0, 0, 1);
            isoctr = new Point3D(0, 0, 0);  //just to initalize
            cameraPosition = new Point3D(0, 0, -3500);
            PlanningItemSummaries = GetPlanningItemSummary(ActivePlanningItem, PlanningItemList);
        }

        public string GetTitle(Patient patient, string scriptVersion)
        {
            Title = patient.Name + " - " + "PlanCheck v." + scriptVersion;
            return Title;
        }

        public ObservableCollection<PQMSummaryViewModel> GetPQMSummaries(ConstraintViewModel constraintPath, PlanningItemViewModel planningItem, Patient patient)
        {
            StructureSet structureSet = planningItem.PlanningItemStructureSet;
            Structure evalStructure;
            ObservableCollection<PQMSummaryViewModel> pqmSummaries = new ObservableCollection<PQMSummaryViewModel>();
            ObservableCollection<StructureViewModel> foundStructureList = new ObservableCollection<StructureViewModel>();
            var calculator = new PQMSummaryCalculator();
            Objectives = calculator.GetObjectives(constraintPath);
            if (planningItem.PlanningItemObject is PlanSum)
            {
                var waitWindowPQM = new WaitWindowPQM();
                PlanSum plansum = (PlanSum)planningItem.PlanningItemObject;
                if (plansum.IsDoseValid() == true)
                {
                    waitWindowPQM.Show();
                    foreach (PQMSummaryViewModel objective in Objectives)
                    {
                        evalStructure = calculator.FindStructureFromAlias(structureSet, objective.TemplateId, objective.TemplateAliases, objective.TemplateCodes);
                        if (evalStructure != null)
                        {
                            var pqmSummary = calculator.CalculatePQM(objective, planningItem, structureSet, new StructureViewModel(evalStructure));
                            pqmSummaries.Add(pqmSummary);
                            foundStructureList.Add(new StructureViewModel(evalStructure));
                        }
                    }
                    FoundStructureList = foundStructureList;
                    waitWindowPQM.Close();
                }
                PqmSummaries = pqmSummaries;
            }
            else //is plansetup
            {
                var waitWindowPQM = new WaitWindowPQM();

                PlanSetup planSetup = (PlanSetup)planningItem.PlanningItemObject;
                if (planSetup.IsDoseValid() == true)
                {
                    waitWindowPQM.Show();
                    foreach (PQMSummaryViewModel objective in Objectives)
                    {
                        evalStructure = calculator.FindStructureFromAlias(structureSet, objective.TemplateId, objective.TemplateAliases, objective.TemplateCodes);
                        if (evalStructure != null)
                        {
                            if (evalStructure.Id.Contains("PTV") == true)
                            {
                                foreach (Structure s in structureSet.Structures)
                                {
                                    if (s.Id == planSetup.TargetVolumeID)
                                        evalStructure = s;
                                }
                            }
                            var PqmSummary = calculator.CalculatePQM(objective, planningItem, structureSet, new StructureViewModel(evalStructure));
                            pqmSummaries.Add(PqmSummary);
                            foundStructureList.Add(new StructureViewModel(evalStructure));
                        }
                    }
                    FoundStructureList = foundStructureList;
                    waitWindowPQM.Close();
                }
                PqmSummaries = pqmSummaries;
            }
            return PqmSummaries;
        }

        public ObservableCollection<PlanningItemDetailsViewModel> GetPlanningItemSummary(PlanningItemViewModel activePlanningItem, ObservableCollection<PlanningItemViewModel> planningItemList)
        {
            var calculator = new PlanningItemDetailsCalculator();
            PlanningItemSummaries = calculator.Calculate(activePlanningItem, planningItemList, PqmSummaries, CollisionSummaries, ErrorGrid);
            return PlanningItemSummaries;
        }

        public List<ErrorViewModel> GetErrors(PlanningItemViewModel planningItem)
        {
            var calculator = new ErrorCalculator();
            ErrorGrid = calculator.Calculate(planningItem.PlanningItemObject);
            ErrorGrid = ErrorGrid.OrderBy(x => x.Status).ToList();
            return ErrorGrid;
        }

        public Tuple<List<CollisionCheckViewModel>, Model3DGroup> GetCollisionSummary(PlanningItemViewModel planningItem)
        {
            var waitWindowCollision = new WaitWindowCollision();

            waitWindowCollision.Show();

            var calculator = new CollisionSummariesCalculator();
            var collimatorModelGroup = new Model3DGroup();
            var isoModelGroup = new Model3DGroup();
            var modelGroup = new Model3DGroup();

            upDir = new Vector3D(0, -1, 0);
            lookDir = new Vector3D(0, 0, 1);
            isoctr = new Point3D(0, 0, 0);  //just to initalize
            cameraPosition = new Point3D(0, 0, -3500);
            var CollisionSummaries = new List<CollisionCheckViewModel>();

            // Create some materials
            var redMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
            var darkblueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkBlue));
            var collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));

            Structure bodyStruct;
            var iso3DMesh = calculator.CalculateIsoMesh(isoctr);
            MeshGeometry3D bodyMesh = null;
            MeshGeometry3D couchMesh = null;
            if (planningItem.PlanningItemObject is PlanSetup)
            {
                PlanSetup planSetup = (PlanSetup)planningItem.PlanningItemObject;
                bodyStruct = planSetup.StructureSet.Structures.Where(x => x.Id.Contains("BODY")).First();
                bodyMesh = bodyStruct.MeshGeometry;
                foreach (Structure structure in planSetup.StructureSet.Structures)
                {
                    if (structure.StructureCodeInfos.FirstOrDefault().Code != null)
                    {
                        if (structure.StructureCodeInfos.FirstOrDefault().Code == "Support")
                        {
                            Structure couchStruct = structure;
                            couchMesh = couchStruct.MeshGeometry;
                        }
                    }
                }
                foreach (Beam beam in planSetup.Beams)
                {
                    isoctr = calculator.GetIsocenter(beam);
                    iso3DMesh = calculator.CalculateIsoMesh(calculator.GetIsocenter(beam));
                    bool view = true;
                    if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne")
                    {
                        upDir = new Vector3D(0, 1, 0);
                    }
                    bool isArc = false;
                    bool isElectron = false;
                    bool isSRSCone = false;
                    collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
                    if (beam.IsSetupField == true)
                    {
                        continue;
                    }
                    if (beam.Name.Contains("Subfield 2") || beam.Name.Contains("Subfield 3"))
                        continue;
                    if (beam.EnergyModeDisplayName.Contains("E"))
                        isElectron = true;
                    if (beam.EnergyModeDisplayName.Contains("SRS"))
                        isSRSCone = true;
                    if (beam.Technique.Id.Contains("ARC"))
                    {
                        isArc = true;
                        collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.GreenYellow));
                    }

                    foreach (Structure structure in planSetup.StructureSet.Structures)
                    {
                        if (structure.Id.Contains("CouchSurface") == true)
                        {
                            Structure couchStruct = planSetup.StructureSet.Structures.Where(x => x.Id.Contains("CouchSurface")).First();
                            couchMesh = couchStruct.MeshGeometry;
                        }
                    }
                    MeshGeometry3D collimatorMesh = calculator.CalculateCollimatorMesh(planSetup, beam, isoctr, isArc, isElectron, isSRSCone);
                    string shortestDistanceBody = "2000000";
                    string shortestDistanceTable = "2000000";
                    string status = "Clear";
                    shortestDistanceBody = calculator.ShortestDistance(collimatorMesh, bodyMesh);
                    if (couchMesh != null)
                        shortestDistanceTable = calculator.ShortestDistance(collimatorMesh, couchMesh);
                    else
                    {
                        shortestDistanceTable = " - ";
                        status = " - ";
                    }
                    Console.WriteLine(beam.Id + " - gantry to body is " + shortestDistanceBody + " cm");
                    Console.WriteLine(beam.Id + " - gantry to table is " + shortestDistanceTable + " cm");
                    if (shortestDistanceTable != " - ")
                    {
                        if ((Convert.ToDouble(shortestDistanceBody) < 3.0) || (Convert.ToDouble(shortestDistanceTable) < 3.0))
                        {
                            collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
                            status = "Collision";
                        }
                    }
                    collimatorModelGroup.Children.Add(new GeometryModel3D { Geometry = collimatorMesh, Material = collimatorMaterial, BackMaterial = darkblueMaterial });
                    isoModelGroup.Children.Add(new GeometryModel3D { Geometry = iso3DMesh, Material = redMaterial, BackMaterial = redMaterial });
                    var collisionSummary = calculator.GetFieldCollisionSummary(beam, view, shortestDistanceBody, shortestDistanceTable, status);
                    CollisionSummaries.Add(collisionSummary);
                }
            }
            modelGroup = CreateModel(bodyMesh, couchMesh, isoModelGroup, collimatorModelGroup, collimatorMaterial);
            waitWindowCollision.Close();
            return Tuple.Create(CollisionSummaries, modelGroup);
        }

        private Model3DGroup CreateModel(MeshGeometry3D bodyMesh, MeshGeometry3D couchMesh, Model3DGroup isoModelGroup, Model3DGroup collimatorModelGroup, Material collimatorMaterial)
        {
            var modelGroup = new Model3DGroup();
            AddModels(bodyMesh, couchMesh, isoModelGroup, collimatorModelGroup, modelGroup, collimatorMaterial);
            return modelGroup;
        }

        private static void AddModels(MeshGeometry3D bodyMesh, MeshGeometry3D couchMesh, Model3DGroup isoModelGroup, Model3DGroup collimatorModelGroup, Model3DGroup modelGroup, Material collimatorMaterial)
        {
            // Create some materials
            var lightblueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));
            var darkblueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkBlue));
            var magentaMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Magenta));

            modelGroup.Children.Add(isoModelGroup);
            modelGroup.Children.Add(collimatorModelGroup);
            modelGroup.Children.Add(new GeometryModel3D { Geometry = bodyMesh, Material = lightblueMaterial, BackMaterial = darkblueMaterial });
            modelGroup.Children.Add(new GeometryModel3D { Geometry = couchMesh, Material = magentaMaterial, BackMaterial = magentaMaterial });
        }
    }
}
