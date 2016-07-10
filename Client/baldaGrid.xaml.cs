using System.Windows.Controls;
using System.Windows.Media;

namespace Balda
{
    /// <summary>
    /// Interaction logic for baldaGrid.xaml
    /// </summary>
    public partial class baldaGrid : UserControl
    {
        public baldaGrid()
        {
            InitializeComponent();
        }

        public string bykva
        {
            set { GridLabel.Content = value; }
            get { return GridLabel.Content.ToString(); }
        }

        public Brush MyBrushSource
        {
            set { MGrid.Background = value; }
        }
    }
}