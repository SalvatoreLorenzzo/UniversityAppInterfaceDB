using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;

namespace UniversityApp
{
    public partial class StudentsControl : UserControl
    {
        private ObservableCollection<Student> students;
        private ObservableCollection<Department> departments;
        private Database db;

        public StudentsControl()
        {
            InitializeComponent();
            db = new Database();
            LoadDepartments();
            LoadStudents();
        }

        private void LoadDepartments()
        {
            departments = new ObservableCollection<Department>(GetDepartmentsFromDatabase());
            DepartmentComboBox.ItemsSource = departments;
        }

        private void LoadStudents()
        {
            students = new ObservableCollection<Student>(GetStudentsFromDatabase());
            StudentsListView.ItemsSource = students;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string enrollmentYearText = EnrollmentYearTextBox.Text.Trim();
            var selectedDepartment = (Department)DepartmentComboBox.SelectedItem;

            // Перевірка на пусті поля
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введіть ім'я студента.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(enrollmentYearText))
            {
                MessageBox.Show("Введіть рік вступу.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedDepartment == null)
            {
                MessageBox.Show("Оберіть кафедру.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка довжини полів
            if (name.Length > 100)
            {
                MessageBox.Show("Ім'я студента не може перевищувати 100 символів.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка унікальності імені
            if (StudentNameExists(name))
            {
                MessageBox.Show("Студент з таким ім'ям вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка року вступу
            if (!int.TryParse(enrollmentYearText, out int enrollmentYear))
            {
                MessageBox.Show("Некоректний рік вступу.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (enrollmentYear < 1900 || enrollmentYear > DateTime.Now.Year)
            {
                MessageBox.Show("Рік вступу має бути між 1900 та поточним роком.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Створення нового студента
            var student = new Student
            {
                Name = name,
                EnrollmentYear = enrollmentYear,
                DepartmentId = selectedDepartment.DepartmentId
            };

            // Додавання до бази даних
            AddStudentToDatabase(student);
            students.Add(student);

            // Очищення полів вводу
            NameTextBox.Clear();
            EnrollmentYearTextBox.Clear();
            DepartmentComboBox.SelectedItem = null;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudent = (Student)StudentsListView.SelectedItem;
            if (selectedStudent != null)
            {
                var result = MessageBox.Show(
                    "Ви впевнені, що хочете видалити студента? Всі пов'язані реєстрації будуть видалені.",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteStudentFromDatabase(selectedStudent);
                    students.Remove(selectedStudent);
                }
            }
            else
            {
                MessageBox.Show("Оберіть студента для видалення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool StudentNameExists(string name)
        {
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT COUNT(*) FROM students WHERE name = @name", db.Connection);
            command.Parameters.AddWithValue("@name", name);
            int count = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
            return count > 0;
        }

        private void AddStudentToDatabase(Student student)
        {
            db.Connection.Open();
            var command = new SQLiteCommand(
                "INSERT INTO students (name, enrollment_year, department_id) VALUES (@name, @enrollment_year, @department_id); SELECT last_insert_rowid();",
                db.Connection);
            command.Parameters.AddWithValue("@name", student.Name);
            command.Parameters.AddWithValue("@enrollment_year", student.EnrollmentYear);
            command.Parameters.AddWithValue("@department_id", student.DepartmentId);
            student.StudentId = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
        }

        private void DeleteStudentFromDatabase(Student student)
        {
            db.Connection.Open();

            // Видалення реєстрацій студента
            var deleteEnrollmentsCommand = new SQLiteCommand(
                "DELETE FROM enrollments WHERE student_id = @student_id",
                db.Connection);
            deleteEnrollmentsCommand.Parameters.AddWithValue("@student_id", student.StudentId);
            deleteEnrollmentsCommand.ExecuteNonQuery();

            // Видалення студента
            var deleteStudentCommand = new SQLiteCommand(
                "DELETE FROM students WHERE student_id = @id",
                db.Connection);
            deleteStudentCommand.Parameters.AddWithValue("@id", student.StudentId);
            deleteStudentCommand.ExecuteNonQuery();

            db.Connection.Close();
        }

        private System.Collections.Generic.List<Student> GetStudentsFromDatabase()
        {
            var list = new System.Collections.Generic.List<Student>();
            db.Connection.Open();
            var command = new SQLiteCommand(@"
        SELECT s.student_id, s.name, s.enrollment_year, s.department_id, d.name
        FROM students s
        LEFT JOIN departments d ON s.department_id = d.department_id", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Student
                {
                    StudentId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    EnrollmentYear = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    DepartmentId = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3),
                    DepartmentName = reader.IsDBNull(4) ? "Не вказано" : reader.GetString(4)
                });
            }
            db.Connection.Close();
            return list;
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
    }
}