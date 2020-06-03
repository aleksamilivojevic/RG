using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using PZ3.Modeli;

namespace PZ3
{
    public partial class MainWindow : Window
    {
        #region POLJA
        private const double SIZE = 10;
        private Point start = new Point();
        private Point diffOffset = new Point();
        private Point mousePosition = new Point();
        private int zoomMax = 7;
        private int zoomCurent = 1;
        private bool rotate = false;
        private bool translate = false;
        private double xScale;
        private double yScale;
        private List<SubstationEntity> substationEntities = new List<SubstationEntity>();
        private List<NodeEntity> nodeEntities = new List<NodeEntity>();
        private List<SwitchEntity> switchEntities = new List<SwitchEntity>();
        private List<LineEntity> lineEntities = new List<LineEntity>();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private AxisAngleRotation3D axisAngleRotation = new AxisAngleRotation3D();
        private GeometryModel3D previousFirstModel;
        private GeometryModel3D previousSecondModel;
        #endregion
        public MainWindow()
        {
            InitializeComponent();

            yScale = 775 / (Pomocna.LAT_MAX - Pomocna.LAT_MIN);
            xScale = 1175 / (Pomocna.LON_MAX - Pomocna.LON_MIN);

            var doc = new XmlDocument();
            doc.Load("Geographic.xml");

            Pomocna.AddEntities(substationEntities, doc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity"));
            Pomocna.AddEntities(nodeEntities, doc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity"));
            Pomocna.AddEntities(switchEntities, doc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity"));
            Pomocna.AddLineEntities(lineEntities, doc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity"));

            substationEntities.ForEach(item =>
            {
                var newY = Pomocna.Convert(item.X, Pomocna.LAT_MIN, yScale);
                var newX = Pomocna.Convert(item.Y, Pomocna.LON_MIN, xScale);
                item.Y = newY;
                item.X = newX;
            });
            nodeEntities.ForEach(item =>
            {
                var newY = Pomocna.Convert(item.X, Pomocna.LAT_MIN, yScale);
                var newX = Pomocna.Convert(item.Y, Pomocna.LON_MIN, xScale);
                item.Y = newY;
                item.X = newX;
            });
            switchEntities.ForEach(item =>
            {
                var newY = Pomocna.Convert(item.X, Pomocna.LAT_MIN, yScale);
                var newX = Pomocna.Convert(item.Y, Pomocna.LON_MIN, xScale);
                item.Y = newY;
                item.X = newX;
            });
            lineEntities.ForEach(item =>
            {
                for (var j = 0; j < item.Vertices.Count; j++)
                {
                    item.Vertices[j] = new Point3D(Pomocna.Convert(item.Vertices[j].Y, Pomocna.LON_MIN, xScale), Pomocna.Convert(item.Vertices[j].X, Pomocna.LAT_MIN, yScale), item.Vertices[j].Z);
                }
            });

            substationEntities.ForEach(item => MakeCube(item, Brushes.Purple));
            nodeEntities.ForEach(item => MakeCube(item, Brushes.Green));
            switchEntities.ForEach(item => MakeCube(item, Brushes.Blue));

            foreach (var item in lineEntities)
            {
                for (var i = 0; i < item.Vertices.Count - 1; i++)
                {
                    MakeLine(0.5, item.Vertices[i], item.Vertices[i + 1], item);
                }
            }
        }

        #region KLASE

        private void MakeLine(double size, Point3D start, Point3D end, LineEntity entity)
        {
            var v = end - start;
            var v1 = new Vector3D(-v.Y, v.X, v.Z);
            var v2 = new Vector3D(v.Y, -v.X, v.Z);
            v1.Normalize();
            v2.Normalize();

            v1 *= size;
            v2 *= size;
            var pointA = start + v1;
            var pointD = start + v2;
            var pointB = end + v1;
            var pointC = end + v2;

            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(pointA);
            mesh.Positions.Add(pointD);
            mesh.Positions.Add(pointB);
            mesh.Positions.Add(pointC);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);

            Brush color;
            color = Brushes.Red;
         
            var material = new DiffuseMaterial(color);
            var model = new GeometryModel3D(mesh, material);
            model.SetValue(TagProperty, entity);
            modelGroup.Children.Add(model);
        }

        private void MakeCube(PowerEntity entity, Brush brush)
        {
            const double HALF_SIZE = SIZE / 2;
            const double HEIGHT = SIZE;

            var mesh = new MeshGeometry3D();

            mesh.Positions.Add(new Point3D(entity.X - HALF_SIZE, entity.Y - HALF_SIZE, HEIGHT - HALF_SIZE)); // dole levo
            mesh.Positions.Add(new Point3D(entity.X - HALF_SIZE, entity.Y - HALF_SIZE, HEIGHT + HALF_SIZE)); 
            mesh.Positions.Add(new Point3D(entity.X + HALF_SIZE, entity.Y - HALF_SIZE, HEIGHT - HALF_SIZE)); // dole desno
            mesh.Positions.Add(new Point3D(entity.X + HALF_SIZE, entity.Y - HALF_SIZE, HEIGHT + HALF_SIZE));
            mesh.Positions.Add(new Point3D(entity.X - HALF_SIZE, entity.Y + HALF_SIZE, HEIGHT - HALF_SIZE)); // gore levo
            mesh.Positions.Add(new Point3D(entity.X - HALF_SIZE, entity.Y + HALF_SIZE, HEIGHT + HALF_SIZE));
            mesh.Positions.Add(new Point3D(entity.X + HALF_SIZE, entity.Y + HALF_SIZE, HEIGHT - HALF_SIZE)); // gore desno
            mesh.Positions.Add(new Point3D(entity.X + HALF_SIZE, entity.Y + HALF_SIZE, HEIGHT + HALF_SIZE)); 

            foreach (var item in modelGroup.Children)
            {
                while (mesh.Bounds.IntersectsWith(item.Bounds))
                {
                    for (var i = 0; i < mesh.Positions.Count; i++)
                    {
                        mesh.Positions[i] = new Point3D(mesh.Positions[i].X, mesh.Positions[i].Y, mesh.Positions[i].Z + HEIGHT);
                    }
                }
            }

            #region Trouglovi


            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(4);
            #endregion


            var material = new DiffuseMaterial(brush);

            var model = new GeometryModel3D(mesh, material);
            model.SetValue(TagProperty, entity);
            modelGroup.Children.Add(model);
        }

        private void Viewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewPort.CaptureMouse();
            start = e.GetPosition(this);
            diffOffset.X = translacija.OffsetX;
            diffOffset.Y = translacija.OffsetY;
            translate = true;
        }

        private void viewPort_MouseMove(object sender, MouseEventArgs e)
        {
            var end = e.GetPosition(this);
            var offsetX = start.X - end.X;
            var offsetY = start.Y - end.Y;
            var translateX = (offsetX * 100) / Width;
            var translateY = -(offsetY * 100) / Height;

            if (viewPort.IsMouseCaptured)
            {
                if (translate)
                {
                    translacija.OffsetX = diffOffset.X + (translateX / (100 * skaliranje.ScaleX)) * 1000;
                    translacija.OffsetY = diffOffset.Y + (translateY / (100 * skaliranje.ScaleY)) * 1000;
                }
                if (rotate)
                {
                    var angleX = (rotateX.Angle + translateY) % 360;
                    rotateY.Angle = (rotateY.Angle + -translateX) % 360;
                    if (-65 < angleX && angleX < 65)
                    {
                        rotateX.Angle = angleX;
                    }
                    start = end;
                }
            }
        }

        private void viewPort_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = PointFromScreen(e.GetPosition(mw));
            skaliranje.CenterX = p.X;
            skaliranje.CenterY = p.Y;
            skaliranje.CenterZ = 0;
            if (e.Delta > 0 && zoomCurent < zoomMax)
            {
                zoomCurent++;
                skaliranje.ScaleX += 0.1;
                skaliranje.ScaleY += 0.1;
                skaliranje.ScaleZ += 0.1;
            }
            else if (e.Delta <= 0 && zoomCurent > -zoomMax)
            {
                zoomCurent--;
                skaliranje.ScaleX -= 0.1;
                skaliranje.ScaleY -= 0.1;
                skaliranje.ScaleZ -= 0.1;
            }
        }

        private void viewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                rotate = true;
                start = e.GetPosition(this);
                viewPort.CaptureMouse();
            }

            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                var mouseposition = e.GetPosition(viewPort);
                var testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
                var testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10);

                var pointparams = new PointHitTestParameters(mouseposition);
                var rayparams = new RayHitTestParameters(testpoint3D, testdirection);

                mousePosition = e.GetPosition(mw);
                VisualTreeHelper.HitTest(viewPort, null, HitResult, pointparams);
            }
        }

        private HitTestResultBehavior HitResult(HitTestResult result)
        {
            var rayResult = result as RayHitTestResult;
            var tag = rayResult?.ModelHit.GetValue(TagProperty);
            if (tag is PowerEntity)
            {
                CreateLabel(mousePosition, tag);
            }
            if (tag is LineEntity)
            {
                ReturnToPreviousColors();

                var line = tag as LineEntity;
                var a = rayResult.ModelHit as GeometryModel3D;
                var first = modelGroup.Children.FirstOrDefault(item => (item.GetValue(TagProperty) as PowerEntity)?.Id == line.FirstEnd);
                var second = modelGroup.Children.FirstOrDefault(item => (item.GetValue(TagProperty) as PowerEntity)?.Id == line.SecondEnd);
                var firstGeometryModel3D = (first as GeometryModel3D);
                var secondGeometryModel3D = (second as GeometryModel3D);
                if (firstGeometryModel3D is object)
                {
                    firstGeometryModel3D.Material = new DiffuseMaterial(Brushes.Pink);
                    previousFirstModel = firstGeometryModel3D;
                }
                if (secondGeometryModel3D is object)
                {
                    secondGeometryModel3D.Material = new DiffuseMaterial(Brushes.Pink);
                    previousSecondModel = secondGeometryModel3D;
                }
            }

            return HitTestResultBehavior.Stop;
        }

        private void ReturnToPreviousColors()
        {
            ReturnToModelToMaterial(previousFirstModel);
            ReturnToModelToMaterial(previousSecondModel);
        }

        private void ReturnToModelToMaterial(GeometryModel3D model3D)
        {
            if (model3D?.GetValue(TagProperty) is SubstationEntity)
            {
                model3D.Material = new DiffuseMaterial(Brushes.Purple);
            }
            if (model3D?.GetValue(TagProperty) is NodeEntity)
            {
                model3D.Material = new DiffuseMaterial(Brushes.Green);
            }
            if (model3D?.GetValue(TagProperty) is SwitchEntity)
            {
                model3D.Material = new DiffuseMaterial(Brushes.Blue);
            }
        }

        private void CreateLabel(Point mousePosition, object entity)
        {
            var label = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            if (entity is PowerEntity)
            {
                label.Content = $"Id: {(entity as PowerEntity).Id}, Name: {(entity as PowerEntity).Name}, Type: {(entity as PowerEntity).GetType().Name}";
            }
            label.Background = Brushes.White;
            label.Margin = new Thickness(mousePosition.X, mousePosition.Y, 0, 0);

            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            label.Arrange(new Rect(label.DesiredSize));

            if (mousePosition.X + label.ActualWidth > mw.Width)
            {
                label.Margin = new Thickness(mousePosition.X - label.ActualWidth, mousePosition.Y, 0, 0);
            }
            if (mousePosition.Y + label.ActualHeight > mw.Height)
            {
                label.Margin = new Thickness(mousePosition.X, mousePosition.Y - label.ActualHeight, 0, 0);
            }

            groupBox.Content = label;
            cts.Cancel();
            cts = new CancellationTokenSource();
            Task.Run(() => RemoveLabelAsync(2), cts.Token);
        }

        private async Task RemoveLabelAsync(int seconds)
        {
            await Task.Delay(seconds * 1000, cts.Token);
            Dispatcher.Invoke(() => { groupBox.Content = null; });
        }

        private void viewPort_MouseUp(object sender, MouseButtonEventArgs e)
        {
            rotate = false;
            translate = false;
            viewPort.ReleaseMouseCapture();
        }
        #endregion
    }
}