using System.Data.SQLite;
using System.IO;

namespace UniversityApp
{
    public class Database
    {
        private const string DatabaseFileName = "university.db";
        private const string ConnectionString = "Data Source=" + DatabaseFileName + ";Version=3;";

        public SQLiteConnection Connection { get; private set; }

        public Database()
        {
            if (!File.Exists(DatabaseFileName))
            {
                SQLiteConnection.CreateFile(DatabaseFileName);
                InitializeDatabase();
            }
            Connection = new SQLiteConnection(ConnectionString);
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                    PRAGMA foreign_keys = ON;

                    CREATE TABLE departments (
                        department_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        building TEXT
                    );

                    CREATE TABLE courses (
                        course_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        department_id INTEGER,
                        credits INTEGER,
                        FOREIGN KEY (department_id) REFERENCES departments(department_id) ON DELETE SET NULL
                    );

                    CREATE TABLE professors (
                        professor_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        department_id INTEGER,
                        FOREIGN KEY (department_id) REFERENCES departments(department_id) ON DELETE SET NULL
                    );

                    CREATE TABLE students (
                        student_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        enrollment_year INTEGER,
                        department_id INTEGER,
                        FOREIGN KEY (department_id) REFERENCES departments(department_id) ON DELETE SET NULL
                    );

                    CREATE TABLE enrollments (
                        enrollment_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        student_id INTEGER,
                        course_id INTEGER,
                        enrollment_date DATE,
                        grade DECIMAL(3,2),
                        FOREIGN KEY (student_id) REFERENCES students(student_id) ON DELETE CASCADE,
                        FOREIGN KEY (course_id) REFERENCES courses(course_id) ON DELETE CASCADE
                    );
                    ";
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }
    }
}