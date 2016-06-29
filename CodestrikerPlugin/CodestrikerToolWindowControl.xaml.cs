//------------------------------------------------------------------------------
// <copyright file="CodestrikerToolWindowControl.xaml.cs" company="FH-Hagenberg">
//     Copyright (c) FH-Hagenberg.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace CodestrikerPlugin
{
    using ViewModels;

    /// <summary>
    /// Interaction logic for CodestrikerToolWindowControl.
    /// </summary>
    public partial class CodestrikerToolWindowControl 
    {
        public CodestrikerPluginViewModel ViewModelContext { get; }
        private bool m_HasFocus = true;
        /// <summary>
        /// Initializes a new instance of the <see cref="CodestrikerToolWindowControl"/> class.
        /// </summary>
        public CodestrikerToolWindowControl()
        {
            InitializeComponent();
            ViewModelContext = CodestrikerPluginViewModel.Instance;
            ViewModelContext.ProjectsUpdated += ViewModelContext_ProjectsUpdated;
            DataContext = ViewModelContext;
            
        }

        private void ViewModelContext_ProjectsUpdated()
        {
            Dispatcher.Invoke(() => { DataContext = ViewModelContext; });
        }

        private void CodestrikerPlugin_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!m_HasFocus)
            {
                ViewModelContext.RefreshCommand.Execute(new object());
            }
            m_HasFocus = false;
        }

        private void CodestrikerPlugin_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            m_HasFocus = true;
        }

      
    }
}