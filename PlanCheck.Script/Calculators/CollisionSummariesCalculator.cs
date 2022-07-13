using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;

namespace PlanCheck
{
    public class CollisionSummariesCalculator
    {
        private DiffuseMaterial redMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
        private DiffuseMaterial darkblueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkBlue));
        private DiffuseMaterial collimatorMaterialStatic = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
        private DiffuseMaterial collimatorMaterialVMAT = new DiffuseMaterial(new SolidColorBrush(Colors.GreenYellow));
        private DiffuseMaterial lightblueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));
        private DiffuseMaterial magentaMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Magenta));

        public CollisionCheckViewModel CalculateBeamCollision(PlanSetup planSetup, Beam beam)
        {
            var calculator = new CollisionSummariesCalculator();
            var modelGroup = new Model3DGroup();
            var isoctr = GetIsocenter(beam);
            bool isVMAT = false;
            bool isStatic = false;
            bool isElectron = false;
            bool isSRSArc = false;
            
            var collisionSummary = new CollisionCheckViewModel();
            MeshGeometry3D bodyMesh;
            var iso3DMesh = CalculateIsoMesh(isoctr);
            var body = planSetup.StructureSet.Structures.Where(x => x.Id.Contains("BODY")).First();
            Structure couch = null;
            MeshGeometry3D couchMesh = null;
            try
            {
                foreach (Structure structure in planSetup.StructureSet.Structures)
                {
                    if (structure.StructureCodeInfos.FirstOrDefault().Code == "Support")
                    {
                        couch = structure;
                        couchMesh = couch.MeshGeometry;
                    }
                }
            }
            catch
            {
                couch = null;
            }
            bodyMesh = body.MeshGeometry;
            var collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
            if (beam.EnergyModeDisplayName.Contains("E"))
                isElectron = true;
            if (beam.EnergyModeDisplayName.Contains("SRS"))
                isSRSArc = true;
            if (beam.MLCPlanType.ToString() == "VMAT" || beam.Technique.Id.Contains("ARC"))
            {
                isVMAT = true;
                collimatorMaterial = collimatorMaterialVMAT;
            }
            if (beam.Technique.ToString().Contains("STATIC"))
            {
                isStatic = true;
                collimatorMaterial = collimatorMaterialStatic;
            }

            MeshGeometry3D collimatorMesh = CalculateCollimatorMesh(beam, isoctr, isVMAT, isStatic, isElectron, isSRSArc);
            collisionSummary = calculator.GetFieldCollisionSummary(beam, couch, body);
            modelGroup.Children.Add(new GeometryModel3D { Geometry = iso3DMesh, Material = redMaterial, BackMaterial = redMaterial });
            modelGroup.Children.Add(new GeometryModel3D { Geometry = collimatorMesh, Material = collimatorMaterial, BackMaterial = darkblueMaterial });
            return collisionSummary;
        }

        public static Model3DGroup AddFieldMesh(PlanSetup planSetup, Beam beam, string status)
        {

            DiffuseMaterial collimatorMaterialStatic = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
            DiffuseMaterial collimatorMaterialVMAT = new DiffuseMaterial(new SolidColorBrush(Colors.GreenYellow));
            if (status == "Collision")
            {
                collimatorMaterialStatic = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
                collimatorMaterialVMAT = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
            }
            if (status == "Warning")
            {
                collimatorMaterialStatic = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));
                collimatorMaterialVMAT = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));
            }
            DiffuseMaterial redMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));

            DiffuseMaterial darkblueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkBlue));
            var isoModelGroup = new Model3DGroup();
            var collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
            var modelGroup = new Model3DGroup();
            bool isVMAT = false;
            bool isStatic = false;
            bool isElectron = false;
            bool isSRSArc = false;
            var upDir = new Vector3D(0, -1, 0);
            var isoctr = GetIsocenter(beam);
            var iso3DMesh = CalculateIsoMesh(isoctr);
            if (beam.EnergyModeDisplayName.Contains("E"))
                isElectron = true;
            if (beam.EnergyModeDisplayName.Contains("SRS"))
                isSRSArc = true;
            if (beam.MLCPlanType.ToString() == "VMAT" || beam.Technique.Id.Contains("ARC"))
            {
                isVMAT = true;
                collimatorMaterial = collimatorMaterialVMAT;
            }
            if (beam.Technique.ToString().Contains("STATIC"))
            {
                isStatic = true;
                collimatorMaterial = collimatorMaterialStatic;
            }
            if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne")
            {
                upDir = new Vector3D(0, 1, 0);

            }
            MeshGeometry3D collimatorMesh = CalculateCollimatorMesh(beam, isoctr, isVMAT, isStatic, isElectron, isSRSArc);
            modelGroup.Children.Add(new GeometryModel3D { Geometry = collimatorMesh, Material = collimatorMaterial, BackMaterial = darkblueMaterial });
            modelGroup.Children.Add(new GeometryModel3D { Geometry = iso3DMesh, Material = redMaterial, BackMaterial = redMaterial });
            modelGroup.Freeze();
            return modelGroup;
        }

        public static Model3DGroup AddCouchBodyMesh(Structure body, Structure couch)
        {
            var modelGroup = new Model3DGroup();
            // Create some materials
            var material = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));
            var darkblueMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkBlue));
            var magentaMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Magenta));

            if (couch != null)
                modelGroup.Children.Add(new GeometryModel3D { Geometry = couch.MeshGeometry, Material = magentaMaterial, BackMaterial = darkblueMaterial });
            modelGroup.Children.Add(new GeometryModel3D { Geometry = body.MeshGeometry, Material = material, BackMaterial = darkblueMaterial });
            modelGroup.Freeze();
            return modelGroup;
        }

        public CollisionCheckViewModel GetFieldCollisionSummary(Beam beam, Structure couch, Structure body)
        {
            Point3D isoctr = GetIsocenter(beam);
            MeshGeometry3D iso3DMesh = CalculateIsoMesh(isoctr);
            bool isVMAT = false;
            bool isStatic = false;
            bool isElectron = false;
            bool isSRSArc = false;
            var collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
            if (beam.EnergyModeDisplayName.Contains("E"))
                isElectron = true;
            if (beam.EnergyModeDisplayName.Contains("SRS"))
                isSRSArc = true;
            if (beam.MLCPlanType.ToString() == "VMAT" || beam.Technique.Id.Contains("ARC"))
            {
                isVMAT = true;
                collimatorMaterial = collimatorMaterialVMAT;
            }
            if (beam.Technique.ToString().Contains("STATIC"))
            {
                isStatic = true;
                collimatorMaterial = collimatorMaterialStatic;
            }
           
            var bodyMesh = body.MeshGeometry;

            MeshGeometry3D collimatorMesh = CalculateCollimatorMesh(beam, isoctr, isVMAT, isStatic, isElectron, isSRSArc);
            string shortestDistanceBody = "2000000";
            string shortestDistanceTable = "2000000";
            string status = "Clear";
            shortestDistanceBody = ShortestDistance(collimatorMesh, bodyMesh);
            if (couch != null)
            {
                var couchMesh = couch.MeshGeometry;
                shortestDistanceTable = ShortestDistance(collimatorMesh, couchMesh);
            }
                
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
                if ((Convert.ToDouble(shortestDistanceBody) < 4.0) || (Convert.ToDouble(shortestDistanceTable) < 4.0))
                {
                    collimatorMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
                    status = "Warning";
                }
            }
            var collisionSummary = new CollisionCheckViewModel
            {
                View = true,
                FieldID = beam.Id,
                GantryToBodyDistance = shortestDistanceBody + " cm",
                GantryToTableDistance = shortestDistanceTable + " cm",
                Status = status
            };
            return collisionSummary;
        }

        public static Point3D GetIsocenter(Beam beam)
        {
            Point3D iso = new Point3D();
            iso.X = beam.IsocenterPosition.x;
            iso.Y = beam.IsocenterPosition.y;
            iso.Z = beam.IsocenterPosition.z;
            return iso;
        }

        public static Point3D GetCameraPosition(Beam beam)
        {
            Point3D cameraPosition = new Point3D();
            cameraPosition.X = beam.IsocenterPosition.x;
            cameraPosition.Y = beam.IsocenterPosition.y;
            cameraPosition.Z = beam.IsocenterPosition.z - 2000;
            return cameraPosition;
        }

        public static MeshGeometry3D CalculateCollimatorMesh(Beam beam, Point3D iso, bool isVMAT, bool isStatic, bool isElectron, bool isSRSArc)
        {
            var meshBuilder = new MeshBuilder(false, false);

            int thetaDiv = 20;

            double tableAngle = beam.ControlPoints.First().PatientSupportAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().PatientSupportAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().PatientSupportAngle - 360.0f);
            double gantryAngle;

            if (isVMAT == true)
            {
                double distToCollFace = 415.0f;
                double collimatorDiameter1 = 470.0f;
                double collimatorDiameter2 = 750.0f;
                double collimatorFaceThickness = 27.5f;
                double collimatorTopThickness = 200.0f;
                int i = 0;
                int arcAngleResolution = 0;  //number of degrees to skip
                foreach (ControlPoint cp in beam.ControlPoints)
                {
                    if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f) : (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f);
                    else
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * cp.GantryAngle : (Math.PI / 180.0f) * (cp.GantryAngle - 360.0f);
                    i++;
                    if ((cp.Index == beam.ControlPoints.First().Index) ||
                        (beam.GantryDirection.ToString() == "Clockwise" && cp.Index == (beam.ControlPoints.First().Index + arcAngleResolution)) ||
                        (beam.GantryDirection.ToString() == "CounterClockwise" && cp.Index == (beam.ControlPoints.First().Index + arcAngleResolution)) ||
                        (cp.Index == beam.ControlPoints.Last().Index))
                    {
                        AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorFaceThickness, iso, collimatorDiameter1);
                        AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorTopThickness, iso, collimatorDiameter2);
                    }
                        
                    if (i > arcAngleResolution)
                        arcAngleResolution = arcAngleResolution + 20;
                }
            }
            if (isStatic == true)
            {
                double distToCollFace = 415.0f;
                double collimatorDiameter1 = 470.0f;
                double collimatorDiameter2 = 750.0f;
                double collimatorFaceThickness = 27.5f;
                double collimatorTopThickness = 200.0f;

                if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f) : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f);
                else
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().GantryAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 360.0f);
                AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorFaceThickness, iso, collimatorDiameter1);
                AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorTopThickness, iso, collimatorDiameter2);
            }
            if (isElectron == true)
            {
                double distToCollFace = 50.0f;
                double fieldSize = Double.Parse(Regex.Match(beam.Applicator.Id, @"\d+").Value);
                double collimaterDiameter = fieldSize * 10.0f + 30.0f; //convert to mm and add 30 mm safety margin
                double collimatorThickness = 30.0f;

                if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f) : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f);
                else
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().GantryAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 360.0f);
                AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorThickness, iso, collimaterDiameter);
            }
            if (isSRSArc == true)
            {
                thetaDiv = 10;
                double distToCollFace = 255.0f;
                double collimatorDiameter = 70.0f;
                double collimatorThickness = 100.0f;
                double gantryAngleLast;

                if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")              
                    gantryAngleLast = beam.ControlPoints.Last().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (beam.ControlPoints.Last().GantryAngle - 180.0f) : (Math.PI / 180.0f) * (beam.ControlPoints.Last().GantryAngle - 180.0f);
                else
                    gantryAngleLast = beam.ControlPoints.Last().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.Last().GantryAngle : (Math.PI / 180.0f) * (beam.ControlPoints.Last().GantryAngle - 360.0f);

                foreach (ControlPoint cp in beam.ControlPoints)
                {
                    if (beam.Plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f) : (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f);
                    else
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * cp.GantryAngle : (Math.PI / 180.0f) * (cp.GantryAngle - 360.0f);                  
                    if ((cp.Index == beam.ControlPoints.First().Index) ||
                        (cp.Index == beam.ControlPoints.Last().Index))
                    {
                        AddCylinderToMesh(meshBuilder, gantryAngle, tableAngle, thetaDiv, distToCollFace, collimatorThickness, iso, collimatorDiameter);
                    }
                    if (beam.GantryDirection.ToString() == "Clockwise" && cp.Index == beam.ControlPoints.First().Index)
                        for (double x = gantryAngle; x < gantryAngleLast; x = x + thetaDiv * (Math.PI / 180))
                            AddCylinderToMesh(meshBuilder, x, tableAngle, thetaDiv, distToCollFace, collimatorThickness, iso, collimatorDiameter);
                        
                    if (beam.GantryDirection.ToString() == "CounterClockwise" && cp.Index == beam.ControlPoints.First().Index)
                        for (double x = gantryAngle; x > gantryAngleLast; x = x - thetaDiv * (Math.PI / 180))
                            AddCylinderToMesh(meshBuilder, x, tableAngle, thetaDiv, distToCollFace, collimatorThickness, iso, collimatorDiameter);
                }
            }
            return meshBuilder.ToMesh(true);
        }

        public static MeshBuilder AddCylinderToMesh(MeshBuilder meshBuilder, double gantryAngle, double tableAngle, int thetaDiv, double distanceFromIso, double cylinderThickness, Point3D iso, double diameter)
        {
            Point3D circleCenter1 = new Point3D();
            Point3D circleCenter2 = new Point3D();

            circleCenter1.X = iso.X + distanceFromIso * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
            circleCenter1.Y = iso.Y - distanceFromIso * Math.Cos(gantryAngle);
            circleCenter1.Z = iso.Z - distanceFromIso * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

            circleCenter2.X = iso.X + (distanceFromIso + cylinderThickness) * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
            circleCenter2.Y = iso.Y - (distanceFromIso + cylinderThickness) * Math.Cos(gantryAngle);
            circleCenter2.Z = iso.Z - (distanceFromIso + cylinderThickness) * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

            meshBuilder.AddCylinder(circleCenter1, circleCenter2, diameter, thetaDiv);
            return meshBuilder;
        }

        public static string ShortestDistance(MeshGeometry3D mesh1, MeshGeometry3D mesh2)
        {
            Rect3D mesh1Bounds = mesh1.Bounds;
            Rect3D mesh2Bounds = mesh2.Bounds;
            Point3DCollection meshPositions1 = new Point3DCollection();
            Point3DCollection meshPositions2 = new Point3DCollection();
            meshPositions1 = mesh1.Positions;
            meshPositions2 = mesh2.Positions;
            double shortestDistance = 2000000;
            foreach (Point3D point1 in meshPositions1)
            {
                foreach (Point3D point2 in meshPositions2)
                {
                    double distance = (Math.Sqrt((Math.Pow((point2.X - point1.X), 2)) + (Math.Pow((point2.Y - point1.Y), 2)) + (Math.Pow((point2.Z - point1.Z), 2)))) / 10;
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                    }
                }
            }
            return shortestDistance.ToString("0.0");
        }

        public static MeshGeometry3D CalculateIsoMesh(Point3D iso)
        {
            var meshBuilder = new MeshBuilder(false, false);
            meshBuilder.AddSphere(iso, 10);

            return meshBuilder.ToMesh(true);
        }
    }
}
