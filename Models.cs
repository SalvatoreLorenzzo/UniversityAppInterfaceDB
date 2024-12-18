namespace UniversityApp
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public string Building { get; set; }
    }

    public class Course
    {
        public int CourseId { get; set; }
        public string Name { get; set; }
        public int? DepartmentId { get; set; }
        public int? Credits { get; set; }
        public string DepartmentName { get; set; }
    }

    public class Professor
    {
        public int ProfessorId { get; set; }
        public string Name { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class Student
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public int? EnrollmentYear { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        public int? StudentId { get; set; }
        public int? CourseId { get; set; }
        public System.DateTime? EnrollmentDate { get; set; }
        public decimal? Grade { get; set; }
    }

    public class EnrollmentViewModel
    {
        public int EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public System.DateTime? EnrollmentDate { get; set; }
        public decimal? Grade { get; set; }

        public string DisplayInfo => $"{StudentName} - {CourseName}";
    }
}