using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;
using System.Linq;

namespace UniversityApp
{
    public partial class CoursesControl : UserControl
    {
        private ObservableCollection<Course> courses;
        private ObservableCollection<Department> departments;
        private Database db;

        public CoursesControl()
        {
            InitializeComponent();
            db = new Database();
            LoadDepartments();
            LoadCourses();
        }

        private void LoadDepartments()
        {
            departments = new ObservableCollection<Department>(GetDepartmentsFromDatabase());
            DepartmentComboBox.ItemsSource = departments;
        }

        private void LoadCourses()
        {
            courses = new ObservableCollection<Course>(GetCoursesFromDatabase());
            CoursesListView.ItemsSource = courses;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string creditsText = CreditsTextBox.Text.Trim();
            var selectedDepartment = (Department)DepartmentComboBox.SelectedItem;

            // Перевірка на пусті поля
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введіть назву курсу.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(creditsText))
            {
                MessageBox.Show("Введіть кількість кредитів.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedDepartment == null)
            {
                MessageBox.Show("Оберіть кафедру.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка довжини назви
            if (name.Length > 100)
            {
                MessageBox.Show("Назва курсу не може перевищувати 100 символів.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка унікальності назви курсу
            if (CourseNameExists(name))
            {
                MessageBox.Show("Курс з такою назвою вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка кредитів
            if (!int.TryParse(creditsText, out int credits))
            {
                MessageBox.Show("Некоректне значення кредитів.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (credits < 0 || credits > 100)
            {
                MessageBox.Show("Кількість кредитів має бути між 0 та 100.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Створення нового курсу
            var course = new Course
            {
                Name = name,
                Credits = credits,
                DepartmentId = selectedDepartment.DepartmentId
            };

            // Додавання до бази даних
            AddCourseToDatabase(course);
            courses.Add(course);

            // Очищення полів вводу
            NameTextBox.Clear();
            CreditsTextBox.Clear();
            DepartmentComboBox.SelectedItem = null;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCourse = (Course)CoursesListView.SelectedItem;
            if (selectedCourse != null)
            {
                var result = MessageBox.Show(
                    "Ви впевнені, що хочете видалити курс? Всі пов'язані реєстрації будуть видалені.",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteCourseFromDatabase(selectedCourse);
                    courses.Remove(selectedCourse);
                }
            }
            else
            {
                MessageBox.Show("Оберіть курс для видалення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CourseNameExists(string name)
        {
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT COUNT(*) FROM courses WHERE name = @name", db.Connection);
            command.Parameters.AddWithValue("@name", name);
            int count = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
            return count > 0;
        }

        private void AddCourseToDatabase(Course course)
        {
            db.Connection.Open();
            var command = new SQLiteCommand(
                "INSERT INTO courses (name, credits, department_id) VALUES (@name, @credits, @department_id); SELECT last_insert_rowid();",
                db.Connection);
            command.Parameters.AddWithValue("@name", course.Name);
            command.Parameters.AddWithValue("@credits", course.Credits);
            command.Parameters.AddWithValue("@department_id", course.DepartmentId);
            course.CourseId = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();

            // Оновлення ObservableCollection departments
            var department = departments.FirstOrDefault(d => d.DepartmentId == course.DepartmentId);

        }


        private void DeleteCourseFromDatabase(Course course)
        {
            db.Connection.Open();

            // Видалення пов'язаних реєстрацій
            var deleteEnrollmentsCommand = new SQLiteCommand(
                "DELETE FROM enrollments WHERE course_id = @course_id",
                db.Connection);
            deleteEnrollmentsCommand.Parameters.AddWithValue("@course_id", course.CourseId);
            deleteEnrollmentsCommand.ExecuteNonQuery();

            // Видалення курсу
            var deleteCourseCommand = new SQLiteCommand(
                "DELETE FROM courses WHERE course_id = @id",
                db.Connection);
            deleteCourseCommand.Parameters.AddWithValue("@id", course.CourseId);
            deleteCourseCommand.ExecuteNonQuery();

            db.Connection.Close();
        }

        private System.Collections.Generic.List<Department> GetDepartmentsFromDatabase()
        {
            var list = new System.Collections.Generic.List<Department>();
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT department_id, name FROM departments", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Department
                {
                    DepartmentId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            db.Connection.Close();
            return list;
        }

        private System.Collections.Generic.List<Course> GetCoursesFromDatabase()
        {
            var list = new System.Collections.Generic.List<Course>();
            db.Connection.Open();
            var command = new SQLiteCommand(@"
        SELECT c.course_id, c.name, c.department_id, c.credits, d.name
        FROM courses c
        LEFT JOIN departments d ON c.department_id = d.department_id", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Course
                {
                    CourseId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DepartmentId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    Credits = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3),
                    DepartmentName = reader.IsDBNull(4) ? "Не вказано" : reader.GetString(4)
                });
            }
            db.Connection.Close();
            return list;
        }
    }
}