using System.Windows.Controls;
using System.Windows.Media;

namespace Balda
{
    /// <summary>
    /// Interaction logic for MyButton.xaml
    /// </summary>
    public partial class MyButton : UserControl
    {
        public Brush NewColor
        {
            set { MyLabel.Foreground = value; }
        }

        public ImageSource NewBrush
        {
            set { MyGrid.Background = new ImageBrush(value); }
        }

        public string Text
        {
            set
            {
                MyLabel.Content = value;
            }
        }

        public MyButton()
        {
            InitializeComponent();
        }
    }
}

