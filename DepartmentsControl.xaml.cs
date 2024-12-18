using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;

namespace UniversityApp
{
    public partial class DepartmentsControl : UserControl
    {
        private ObservableCollection<Department> departments;
        private Database db;

        public DepartmentsControl()
        {
            InitializeComponent();
            db = new Database();
            LoadDepartments();
        }

        private void LoadDepartments()
        {
            departments = new ObservableCollection<Department>(GetDepartmentsFromDatabase());
            DepartmentsListView.ItemsSource = departments;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string building = BuildingTextBox.Text.Trim();

            // Перевірка на пусті поля
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введіть назву кафедри.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка довжини полів
            if (name.Length > 100)
            {
                MessageBox.Show("Назва кафедри не може перевищувати 100 символів.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (building.Length > 50)
            {
                MessageBox.Show("Назва будівлі не може перевищувати 50 символів.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка унікальності назви кафедри
            if (DepartmentNameExists(name))
            {
                MessageBox.Show("Кафедра з такою назвою вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Створення нової кафедри
            var department = new Department
            {
                Name = name,
                Building = string.IsNullOrWhiteSpace(building) ? null : building
            };

            // Додавання до бази даних
            AddDepartmentToDatabase(department);
            departments.Add(department);

            // Очищення полів вводу
            NameTextBox.Clear();
            BuildingTextBox.Clear();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDepartment = (Department)DepartmentsListView.SelectedItem;
            if (selectedDepartment != null)
            {
                var result = MessageBox.Show(
                    "Ви впевнені, що хочете видалити кафедру? Всі пов'язані курси, студенти та викладачі будуть видалені.",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteDepartmentFromDatabase(selectedDepartment);
                    departments.Remove(selectedDepartment);
                }
            }
            else
            {
                MessageBox.Show("Оберіть кафедру для видалення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool DepartmentNameExists(string name)
        {
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT COUNT(*) FROM departments WHERE name = @name", db.Connection);
            command.Parameters.AddWithValue("@name", name);
            int count = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
            return count > 0;
        }

        private void AddDepartmentToDatabase(Department department)
        {
            db.Connection.Open();
            var command = new SQLiteCommand(
                "INSERT INTO departments (name, building) VALUES (@name, @building); SELECT last_insert_rowid();",
                db.Connection);
            command.Parameters.AddWithValue("@name", department.Name);
            command.Parameters.AddWithValue("@building", department.Building);
            department.DepartmentId = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
        }

        private void DeleteDepartmentFromDatabase(Department department)
        {
            db.Connection.Open();

            // Видалення реєстрацій студентів, пов'язаних з кафедрою
            var deleteEnrollmentsCommand = new SQLiteCommand(
                @"DELETE FROM enrollments WHERE student_id IN (
                    SELECT student_id FROM students WHERE department_id = @department_id
                );",
                db.Connection);
            deleteEnrollmentsCommand.Parameters.AddWithValue("@department_id", department.DepartmentId);
            deleteEnrollmentsCommand.ExecuteNonQuery();

            // Видалення студентів
            var deleteStudentsCommand = new SQLiteCommand(
                "DELETE FROM students WHERE department_id = @department_id",
                db.Connection);
            deleteStudentsCommand.Parameters.AddWithValue("@department_id", department.DepartmentId);
            deleteStudentsCommand.ExecuteNonQuery();

            // Видалення курсів
            var deleteCoursesCommand = new SQLiteCommand(
                "DELETE FROM courses WHERE department_id = @department_id",
                db.Connection);
            deleteCoursesCommand.Parameters.AddWithValue("@department_id", department.DepartmentId);
            deleteCoursesCommand.ExecuteNonQuery();

            // Видалення реєстрацій курсів
            var deleteCourseEnrollmentsCommand = new SQLiteCommand(
                @"DELETE FROM enrollments WHERE course_id IN (
                    SELECT course_id FROM courses WHERE department_id = @department_id
                );",
                db.Connection);
            deleteCourseEnrollmentsCommand.Parameters.AddWithValue("@department_id", department.DepartmentId);
            deleteCourseEnrollmentsCommand.ExecuteNonQuery();

            // Видалення викладачів
            var deleteProfessorsCommand = new SQLiteCommand(
                "DELETE FROM professors WHERE department_id = @department_id",
                db.Connection);
            deleteProfessorsCommand.Parameters.AddWithValue("@department_id", department.DepartmentId);
            deleteProfessorsCommand.ExecuteNonQuery();

            // Видалення кафедри
            var deleteDepartmentCommand = new SQLiteCommand(
                "DELETE FROM departments WHERE department_id = @id",
                db.Connection);
            deleteDepartmentCommand.Parameters.AddWithValue("@id", department.DepartmentId);
            deleteDepartmentCommand.ExecuteNonQuery();

            db.Connection.Close();
        }

        private System.Collections.Generic.List<Department> GetDepartmentsFromDatabase()
        {
            var list = new System.Collections.Generic.List<Department>();
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT department_id, name, building FROM departments", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Department
                {
                    DepartmentId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Building = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            db.Connection.Close();
            return list;
        }
    }
}