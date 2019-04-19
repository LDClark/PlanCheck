using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;
using System.Threading;
using System.Threading.Tasks;

namespace PlanCheck
{
    public class CollisionSummariesCalculator
    {
        public CollisionCheckViewModel GetFieldCollisionSummary(Beam beam, bool view, string shortestDistanceBody, string shortestDistanceTable, string status)
        {
            var collisionSummary = new CollisionCheckViewModel
            {
                View = view,
                FieldID = beam.Id,
                GantryToBodyDistance = shortestDistanceBody + " cm",
                GantryToTableDistance = shortestDistanceTable + " cm",
                Status = status
            };
            return collisionSummary;
        }

        public Point3D GetIsocenter(Beam beam)
        {
            Point3D iso = new Point3D();
            iso.X = beam.IsocenterPosition.x;
            iso.Y = beam.IsocenterPosition.y;
            iso.Z = beam.IsocenterPosition.z;
            return iso;
        }

        public Point3D GetCameraPosition(Beam beam)
        {
            Point3D cameraPosition = new Point3D();
            cameraPosition.X = beam.IsocenterPosition.x;
            cameraPosition.Y = beam.IsocenterPosition.y;
            cameraPosition.Z = beam.IsocenterPosition.z - 3500;
            return cameraPosition;
        }

        public MeshGeometry3D CalculateCollimatorMesh(PlanSetup planSetup, Beam beam, Point3D iso, bool isArc, bool isElectron, bool isSRSCone)
        {
            var meshBuilder = new MeshBuilder(false, false);

            int thetaDiv = 20;

            double tableAngle = beam.ControlPoints.First().PatientSupportAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().PatientSupportAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().PatientSupportAngle - 360.0f);
            double gantryAngle;

            if (isArc == true)
            {
                int i = 0;
                int arcAngleResolution = 0;  //number of degrees to skip
                foreach (ControlPoint cp in beam.ControlPoints)
                {
                    if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne")
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f) : (Math.PI / 180.0f) * (cp.GantryAngle - 180.0f);
                    else
                        gantryAngle = cp.GantryAngle <= 180.0f ? (Math.PI / 180.0f) * cp.GantryAngle : (Math.PI / 180.0f) * (cp.GantryAngle - 360.0f);
                    i++;
                    if (cp.Index == beam.ControlPoints.First().Index)
                        AddCylinderToMesh(planSetup, meshBuilder, gantryAngle, tableAngle, iso);
                    if (beam.GantryDirection.ToString() == "Clockwise")
                    {
                        if (cp.Index == (beam.ControlPoints.First().Index + arcAngleResolution))
                            AddCylinderToMesh(planSetup, meshBuilder, gantryAngle, tableAngle, iso);
                    }                       
                    if (beam.GantryDirection.ToString() == "CounterClockwise")
                    {
                        if (cp.Index == (beam.ControlPoints.First().Index - arcAngleResolution))
                            AddCylinderToMesh(planSetup, meshBuilder, gantryAngle, tableAngle, iso);
                    }
                    if (cp.Index == beam.ControlPoints.Last().Index)
                        AddCylinderToMesh(planSetup, meshBuilder, gantryAngle, tableAngle, iso);
                    if (i > arcAngleResolution)
                        arcAngleResolution = arcAngleResolution + 20;
                }
            }
            else
            {
                if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne")
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f) : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f);
                else
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().GantryAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 360.0f);

                AddCylinderToMesh(planSetup, meshBuilder, gantryAngle, tableAngle, iso);

            }
            if (isElectron == true) //todo
            {
                double distToConeElectron = 5.0f;
                double fieldSize = Double.Parse(Regex.Match(beam.Applicator.Id, @"\d+").Value);
                double coneDiameter = fieldSize + 3.0f;
                double coneThickness = 3.0f;
                Point3D coneCenter = new Point3D();
                Point3D coneTop = new Point3D();

                if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne")
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f) : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f);
                else
                    gantryAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().GantryAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 360.0f);


                coneCenter.X = iso.X + (distToConeElectron) * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
                coneCenter.Y = iso.Y - (distToConeElectron) * Math.Cos(gantryAngle);
                coneCenter.Z = iso.Z - (distToConeElectron) * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

                coneTop.X = iso.X + (distToConeElectron + coneThickness) * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
                coneTop.Y = iso.Y - (distToConeElectron + coneThickness) * Math.Cos(gantryAngle);
                coneTop.Z = iso.Z - (distToConeElectron + coneThickness) * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

                meshBuilder.AddCylinder(coneCenter, coneTop, coneDiameter, thetaDiv);
            }
            if (isSRSCone == true) //todo
            {
                double distToConeSRS = 34.6f;
                double coneDiameter = 5.0f;
                double coneThickness = 10.0f;
                Point3D coneCenter = new Point3D();
                Point3D coneTop = new Point3D();

                if (planSetup.TreatmentOrientation.ToString() == "HeadFirstProne")
                    tableAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f) : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 180.0f);
                else
                    tableAngle = beam.ControlPoints.First().GantryAngle <= 180.0f ? (Math.PI / 180.0f) * beam.ControlPoints.First().GantryAngle : (Math.PI / 180.0f) * (beam.ControlPoints.First().GantryAngle - 360.0f);

                coneCenter.X = iso.X + (distToConeSRS) * Math.Cos(tableAngle) * Math.Sin(tableAngle);
                coneCenter.Y = iso.Y - (distToConeSRS) * Math.Cos(tableAngle);
                coneCenter.Z = iso.Z - (distToConeSRS) * Math.Sin(tableAngle) * Math.Sin(tableAngle);

                coneTop.X = iso.X + (distToConeSRS + coneThickness) * Math.Cos(tableAngle) * Math.Sin(tableAngle);
                coneTop.Y = iso.Y - (distToConeSRS + coneThickness) * Math.Cos(tableAngle);
                coneTop.Z = iso.Z - (distToConeSRS + coneThickness) * Math.Sin(tableAngle) * Math.Sin(tableAngle);

                meshBuilder.AddCylinder(coneCenter, coneTop, coneDiameter, thetaDiv);
            }

            return meshBuilder.ToMesh(true);
        }

        public MeshBuilder AddCylinderToMesh(PlanSetup planSetup, MeshBuilder meshBuilder, double gantryAngle, double tableAngle, Point3D iso)
        {
            int thetaDiv = 20;
            Point3D collimatorPlate = new Point3D();
            Point3D collimatorCenter = new Point3D();
            Point3D collimatorTop = new Point3D();

            double distToCollFace = 415.0f;
            double collimatorDiameter1 = 470.0f;
            double collimatorDiameter2 = 750.0f;
            double collimatorFaceThickness = 27.5f;
            double collimatorTopThickness = 200.0f;

            collimatorPlate.X = iso.X + distToCollFace * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
            collimatorPlate.Y = iso.Y - distToCollFace * Math.Cos(gantryAngle);
            collimatorPlate.Z = iso.Z - distToCollFace * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

            collimatorCenter.X = iso.X + (distToCollFace + collimatorFaceThickness) * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
            collimatorCenter.Y = iso.Y - (distToCollFace + collimatorFaceThickness) * Math.Cos(gantryAngle);
            collimatorCenter.Z = iso.Z - (distToCollFace + collimatorFaceThickness) * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

            collimatorTop.X = iso.X + (distToCollFace + collimatorTopThickness) * Math.Cos(tableAngle) * Math.Sin(gantryAngle);
            collimatorTop.Y = iso.Y - (distToCollFace + collimatorTopThickness) * Math.Cos(gantryAngle);
            collimatorTop.Z = iso.Z - (distToCollFace + collimatorTopThickness) * Math.Sin(tableAngle) * Math.Sin(gantryAngle);

            meshBuilder.AddCylinder(collimatorPlate, collimatorCenter, collimatorDiameter1, thetaDiv);
            meshBuilder.AddCylinder(collimatorCenter, collimatorTop, collimatorDiameter2, thetaDiv);

            return meshBuilder;
        }

        public string ShortestDistance(MeshGeometry3D mesh1, MeshGeometry3D mesh2)
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

        public MeshGeometry3D CalculateIsoMesh(Point3D iso)
        {
            var meshBuilder = new MeshBuilder(false, false);
            meshBuilder.AddSphere(iso, 10);

            return meshBuilder.ToMesh(true);
        }
    }
}
