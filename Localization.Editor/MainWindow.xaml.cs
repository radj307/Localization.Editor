using Localization.Editor.ViewModels;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Localization.Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ViewModel = new();

            InitializeComponent();

            NodeNavigator.SelectedItemChanged += this.NodeNavigator_SelectedItemChanged;
        }

        public MainWindowVM ViewModel { get; }
        public IEnumerable<string> PathBoxAutocompleteSource { get; private set; }

        private void NodeNavigator_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
            => ViewModel.UpdateCurrentPath((TreeNode?)e.NewValue);

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Open Translation Config(s)",
                CheckFileExists = true,
            };
            if (ofd.ShowDialog() == true)
            {
                foreach (var file in ofd.FileNames)
                {
                    if (file.Length == 0) continue;
                    if (File.Exists(file))
                    {
                        try
                        {
                            Loc.Instance.LoadFromFile(file);
                        }
                        catch { }
                    }
                }
            }
        }

        private void PathBox_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            
        }
    }
}
