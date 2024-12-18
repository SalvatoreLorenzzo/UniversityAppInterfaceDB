using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;

namespace UniversityApp
{
    public partial class ProfessorsControl : UserControl
    {
        private ObservableCollection<Professor> professors;
        private ObservableCollection<Department> departments;
        private Database db;

        public ProfessorsControl()
        {
            InitializeComponent();
            db = new Database();
            LoadDepartments();
            LoadProfessors();
        }

        private void LoadDepartments()
        {
            departments = new ObservableCollection<Department>(GetDepartmentsFromDatabase());
            DepartmentComboBox.ItemsSource = departments;
        }

        private void LoadProfessors()
        {
            professors = new ObservableCollection<Professor>(GetProfessorsFromDatabase());
            ProfessorsListView.ItemsSource = professors;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            var selectedDepartment = (Department)DepartmentComboBox.SelectedItem;

            // Перевірка на пусті поля
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введіть ім'я викладача.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedDepartment == null)
            {
                MessageBox.Show("Оберіть кафедру.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка довжини
            if (name.Length > 100)
            {
                MessageBox.Show("Ім'я викладача не може перевищувати 100 символів.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка унікальності імені
            if (ProfessorNameExists(name))
            {
                MessageBox.Show("Викладач з таким ім'ям вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Створення нового викладача
            var professor = new Professor
            {
                Name = name,
                DepartmentId = selectedDepartment.DepartmentId
            };

            // Додавання до бази даних
            AddProfessorToDatabase(professor);
            professors.Add(professor);

            // Очищення полів вводу
            NameTextBox.Clear();
            DepartmentComboBox.SelectedItem = null;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfessor = (Professor)ProfessorsListView.SelectedItem;
            if (selectedProfessor != null)
            {
                var result = MessageBox.Show(
                    "Ви впевнені, що хочете видалити викладача? Всі пов'язані записи також будуть видалені.",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteProfessorFromDatabase(selectedProfessor);
                    professors.Remove(selectedProfessor);
                }
            }
            else
            {
                MessageBox.Show("Оберіть викладача для видалення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ProfessorNameExists(string name)
        {
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT COUNT(*) FROM professors WHERE name = @name", db.Connection);
            command.Parameters.AddWithValue("@name", name);
            int count = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
            return count > 0;
        }

        private void AddProfessorToDatabase(Professor professor)
        {
            db.Connection.Open();
            var command = new SQLiteCommand(
                "INSERT INTO professors (name, department_id) VALUES (@name, @department_id); SELECT last_insert_rowid();",
                db.Connection);
            command.Parameters.AddWithValue("@name", professor.Name);
            command.Parameters.AddWithValue("@department_id", professor.DepartmentId);
            professor.ProfessorId = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
        }

        private void DeleteProfessorFromDatabase(Professor professor)
        {
            db.Connection.Open();

            // Видалення викладача
            var deleteProfessorCommand = new SQLiteCommand(
                "DELETE FROM professors WHERE professor_id = @id",
                db.Connection);
            deleteProfessorCommand.Parameters.AddWithValue("@id", professor.ProfessorId);
            deleteProfessorCommand.ExecuteNonQuery();

            db.Connection.Close();
        }

        private System.Collections.Generic.List<Professor> GetProfessorsFromDatabase()
        {
            var list = new System.Collections.Generic.List<Professor>();
            db.Connection.Open();
            var command = new SQLiteCommand(@"
        SELECT p.professor_id, p.name, p.department_id, d.name
        FROM professors p
        LEFT JOIN departments d ON p.department_id = d.department_id", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Professor
                {
                    ProfessorId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DepartmentId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    DepartmentName = reader.IsDBNull(3) ? "Не вказано" : reader.GetString(3)
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