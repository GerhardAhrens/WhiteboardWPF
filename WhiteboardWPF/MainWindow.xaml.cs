namespace WhiteboardWPF
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.StatusText.Text = "Whiteboard bereit";
        }

        private void OnCloseContextMenu(object sender, RoutedEventArgs e)
        {

        }

        private void OnResetBoardClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnClearBoardClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnAddArrowClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnAddTextClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnAddShapeClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnNewBoardClick(object sender, RoutedEventArgs e)
        {
            if (WhiteBoardCanvas.Children.Count > 0)
            {
                MessageBoxResult result =  MessageBox.Show("Das aktuelle Whiteboard wird geleert.\n\nFortfahren?",
                        "Neues Whiteboard",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            WhiteBoardCanvas.Children.Clear();

            StatusText.Text = "Neues Whiteboard erstellt";
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnWhiteBoardCanvasMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
         /*
         * Das Contextmenü wird automatisch durch WPF geöffnet.
         *
         * Wir setzen hier lediglich den Status.
         */

            Point position = e.GetPosition(WhiteBoardCanvas);

            this.StatusText.Text = $"Position: X={position.X:0}, Y={position.Y:0}";
        }
    }
}