using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PZ3.Modeli
{
    public class Dodatni
    {
        public PowerEntity PowerEntity { get; set; }
        public GeometryModel3D Model3D { get; set; }

        private int brojkonekcija;

        public int BrojKonekcija
        {
            get => brojkonekcija;
            set
            {
                brojkonekcija = value;

                if (Model3D == null)
                    return;

                UpdateModelColor();
            }
        }

        private bool oznacen;

        public bool Oznacen
        {
            get => oznacen;
            set
            {
                oznacen = value;
                UpdateModelColor();
            }
        }

        public void UpdateModelColor()
        {
            if (oznacen)
            {
                Model3D.Material = new DiffuseMaterial(Brushes.Blue);
            }
            else if (brojkonekcija < 3)
            {
                Model3D.Material = new DiffuseMaterial(Brushes.PaleVioletRed);
            }
            else if (brojkonekcija <= 5)
            {
                Model3D.Material = new DiffuseMaterial(Brushes.Red);
            }
            else
            {
                Model3D.Material = new DiffuseMaterial(Brushes.DarkRed);
            }
        }
    }
}
