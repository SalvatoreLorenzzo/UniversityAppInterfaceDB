using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;

namespace UniversityApp
{
    public partial class EnrollmentsControl : UserControl
    {
        private ObservableCollection<EnrollmentViewModel> enrollments;
        private ObservableCollection<Student> students;
        private ObservableCollection<Course> courses;
        private Database db;

        public EnrollmentsControl()
        {
            InitializeComponent();
            db = new Database();
            LoadStudents();
            LoadCourses();
            LoadEnrollments();
        }

        private void LoadStudents()
        {
            students = new ObservableCollection<Student>(GetStudentsFromDatabase());
            StudentComboBox.ItemsSource = students;
        }

        private void LoadCourses()
        {
            courses = new ObservableCollection<Course>(GetCoursesFromDatabase());
            CourseComboBox.ItemsSource = courses;
        }

        private void LoadEnrollments()
        {
            enrollments = new ObservableCollection<EnrollmentViewModel>(GetEnrollmentsFromDatabase());
            EnrollmentsListView.ItemsSource = enrollments;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudent = (Student)StudentComboBox.SelectedItem;
            var selectedCourse = (Course)CourseComboBox.SelectedItem;
            string gradeText = GradeTextBox.Text.Trim();
            DateTime? enrollmentDate = EnrollmentDatePicker.SelectedDate;

            // Перевірка на пусті поля
            if (selectedStudent == null)
            {
                MessageBox.Show("Оберіть студента.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedCourse == null)
            {
                MessageBox.Show("Оберіть курс.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!enrollmentDate.HasValue)
            {
                MessageBox.Show("Виберіть дату реєстрації.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(gradeText))
            {
                MessageBox.Show("Введіть оцінку.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка дати реєстрації
            if (enrollmentDate.Value > DateTime.Now)
            {
                MessageBox.Show("Дата реєстрації не може бути в майбутньому.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка оцінки
            if (!decimal.TryParse(gradeText, out decimal grade))
            {
                MessageBox.Show("Некоректне значення оцінки.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (grade < 0 || grade > 100)
            {
                MessageBox.Show("Оцінка має бути між 0 та 100.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка наявності вже існуючого запису
            if (EnrollmentExists(selectedStudent.StudentId, selectedCourse.CourseId))
            {
                MessageBox.Show("Цей студент вже зареєстрований на цей курс.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Створення нової реєстрації
            var enrollment = new Enrollment
            {
                StudentId = selectedStudent.StudentId,
                CourseId = selectedCourse.CourseId,
                EnrollmentDate = enrollmentDate,
                Grade = grade
            };

            // Додавання до бази даних
            AddEnrollmentToDatabase(enrollment);
            enrollments.Add(new EnrollmentViewModel
            {
                EnrollmentId = enrollment.EnrollmentId,
                StudentName = selectedStudent.Name,
                CourseName = selectedCourse.Name,
                EnrollmentDate = enrollment.EnrollmentDate,
                Grade = enrollment.Grade
            });

            // Очищення полів вводу
            StudentComboBox.SelectedItem = null;
            CourseComboBox.SelectedItem = null;
            EnrollmentDatePicker.SelectedDate = null;
            GradeTextBox.Clear();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedEnrollment = (EnrollmentViewModel)EnrollmentsListView.SelectedItem;
            if (selectedEnrollment != null)
            {
                var result = MessageBox.Show(
                    "Ви впевнені, що хочете видалити реєстрацію?",
                    "Підтвердження видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteEnrollmentFromDatabase(selectedEnrollment);
                    enrollments.Remove(selectedEnrollment);
                }
            }
            else
            {
                MessageBox.Show("Оберіть реєстрацію для видалення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool EnrollmentExists(int studentId, int courseId)
        {
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT COUNT(*) FROM enrollments WHERE student_id = @student_id AND course_id = @course_id", db.Connection);
            command.Parameters.AddWithValue("@student_id", studentId);
            command.Parameters.AddWithValue("@course_id", courseId);
            int count = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
            return count > 0;
        }

        private void AddEnrollmentToDatabase(Enrollment enrollment)
        {
            db.Connection.Open();
            var command = new SQLiteCommand(
                "INSERT INTO enrollments (student_id, course_id, enrollment_date, grade) VALUES (@student_id, @course_id, @enrollment_date, @grade); SELECT last_insert_rowid();",
                db.Connection);
            command.Parameters.AddWithValue("@student_id", enrollment.StudentId);
            command.Parameters.AddWithValue("@course_id", enrollment.CourseId);
            command.Parameters.AddWithValue("@enrollment_date", enrollment.EnrollmentDate?.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@grade", enrollment.Grade);
            enrollment.EnrollmentId = Convert.ToInt32(command.ExecuteScalar());
            db.Connection.Close();
        }

        private void DeleteEnrollmentFromDatabase(EnrollmentViewModel enrollment)
        {
            db.Connection.Open();
            var command = new SQLiteCommand(
                "DELETE FROM enrollments WHERE enrollment_id = @id",
                db.Connection);
            command.Parameters.AddWithValue("@id", enrollment.EnrollmentId);
            command.ExecuteNonQuery();
            db.Connection.Close();
        }

        private System.Collections.Generic.List<EnrollmentViewModel> GetEnrollmentsFromDatabase()
        {
            var list = new System.Collections.Generic.List<EnrollmentViewModel>();
            db.Connection.Open();
            var command = new SQLiteCommand(@"
                SELECT e.enrollment_id, s.name, c.name, e.enrollment_date, e.grade
                FROM enrollments e
                LEFT JOIN students s ON e.student_id = s.student_id
                LEFT JOIN courses c ON e.course_id = c.course_id", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new EnrollmentViewModel
                {
                    EnrollmentId = reader.GetInt32(0),
                    StudentName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    CourseName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    EnrollmentDate = reader.IsDBNull(3) ? null : (DateTime?)DateTime.Parse(reader.GetString(3)),
                    Grade = reader.IsDBNull(4) ? null : (decimal?)reader.GetDecimal(4)
                });
            }
            db.Connection.Close();
            return list;
        }

        private System.Collections.Generic.List<Student> GetStudentsFromDatabase()
        {
            var list = new System.Collections.Generic.List<Student>();
            db.Connection.Open();
            var command = new SQLiteCommand("SELECT student_id, name FROM students", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Student
                {
                    StudentId = reader.GetInt32(0),
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
            var command = new SQLiteCommand("SELECT course_id, name FROM courses", db.Connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Course
                {
                    CourseId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            db.Connection.Close();
            return list;
        }
    }
}