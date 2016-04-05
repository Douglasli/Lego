using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gqqnbig.Lego
{
    /// <summary>
    /// MapWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MapWindow : Window
    {
        public VehicleState destVehicleState;

        public Map map;

        public VehicleState startingVehicleState;


        public CarPainter drawer;

        public MapWindow()
        {
            InitializeComponent();

        }

        private void solveButton_Click(object sender, RoutedEventArgs e)
        {
            ParkingSolver solver = new ParkingSolver(2, 10, destVehicleState.Center, destVehicleState.Orientation, map);
            historyListBox.ItemsSource = solver.Solve(startingVehicleState);
        }

        private void setDestButton_Click(object sender, RoutedEventArgs e)
        {

            destVehicleState = new VehicleState(
                new Point(Convert.ToDouble(xTextBox.Text), Convert.ToDouble(yTextBox.Text)),
                Convert.ToDouble(orientationTextBox.Text));

            Draw();
        }

        public void Draw()
        {

            canvas.Children.Clear();
            if (map != null) 
                drawer.Draw(map);
            if (startingVehicleState != null) 
                drawer.Draw(startingVehicleState);
            if (destVehicleState != null)
                drawer.DrawEndPose(destVehicleState);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            drawer = new CarPainter(canvas);
        }

    }
}
