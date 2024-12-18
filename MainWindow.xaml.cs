using System.Windows;

namespace UniversityApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new DepartmentsControl();
        }

        private void DepartmentsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DepartmentsControl();
        }

        private void CoursesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new CoursesControl();
        }

        private void StudentsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new StudentsControl();
        }

        private void ProfessorsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ProfessorsControl();
        }

        private void EnrollmentsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new EnrollmentsControl();
        }
    }
}