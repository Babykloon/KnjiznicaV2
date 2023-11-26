using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static WinFormsApp8.Form1;

namespace WinFormsApp8
{
    public partial class Form1 : Form
    {
        private Library library;
        private CodeGenerator bookcodes;
        private CodeGenerator studentcodes;
        private string connectionString = "Data Source=DESKTOP-76L4HIG;Initial Catalog=KnjiznicaV3;Persist Security Info=True;User ID=Karlo;Password=***********;Pooling=False; TrustServerCertificate=True; Trusted_Connection=True";






        public Form1()
        {
            InitializeComponent();
            library = new Library();
            bookcodes = new CodeGenerator();


            LoadBooksComboBox();
            LoadStudentsComboBox();


            label2.Text = "Ime";

            label3.Text = "Autor";

            label4.Text = "Kolièina";


        }




        private void buttonAddBook_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxBookName.Text) || string.IsNullOrWhiteSpace(textBoxBookAuthor.Text) || string.IsNullOrWhiteSpace(textBoxBookQuantity.Text))
            {
                MessageBox.Show("Polja ne smiju biti prazna.");
            }
            else
            {
                try
                {
                    string code = CodeGenerator.GenerateUniqueCode(library.GetBooks().Select(b => b.Code).ToList());
                    string name = textBoxBookName.Text;
                    string author = textBoxBookAuthor.Text;
                    int quantity;

                    if (!int.TryParse(textBoxBookQuantity.Text, out quantity))
                    {
                        MessageBox.Show("Kolièina mora biti valjani broj.");
                        return;
                    }

                    library.AddBook(code, name, author, quantity);

                    LoadBooksComboBox();
                    
                }
                catch
                {
                    MessageBox.Show("Greška u upisu podataka.");
                }
            }
        }


        private void buttonReturnBook_Click(object sender, EventArgs e)
        {
            if (comboBoxStudents.SelectedItem == null || comboBoxBooks.SelectedItem == null)
            {
                MessageBox.Show("Odaberite knjigu i studenta.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string selectedStudentName = comboBoxStudents.SelectedItem.ToString();
                string[] studentDetails = selectedStudentName.Split('-');
                string studentName = studentDetails[0].Trim();
                string bookDetails = comboBoxBooks.SelectedItem.ToString();

                string bookCode = bookDetails.Substring(bookDetails.LastIndexOf("Code:") + 6).Trim();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM BorrowedBooks " +
                                        "JOIN Students ON BorrowedBooks.StudentId = Students.Id " +
                                        "WHERE BorrowedBooks.BookCode = @BookCode AND Students.Name = @StudentName";

                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@BookCode", bookCode);
                        checkCommand.Parameters.AddWithValue("@StudentName", studentName);

                        int count = (int)checkCommand.ExecuteScalar();

                        if (count == 0)
                        {
                            MessageBox.Show($"{studentName} nije posudio knjigu {bookDetails}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    string returnQuery = "DELETE FROM BorrowedBooks " +
                                         "WHERE StudentId = (SELECT Id FROM Students WHERE Name = @StudentName) " +
                                         "AND BookCode = @BookCode";

                    using (SqlCommand returnCommand = new SqlCommand(returnQuery, connection))
                    {
                        returnCommand.Parameters.AddWithValue("@StudentName", studentName);
                        returnCommand.Parameters.AddWithValue("@BookCode", bookCode);

                        returnCommand.ExecuteNonQuery();
                        MessageBox.Show($"{studentName} je uspješno vratio knjigu {bookDetails}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    string updateCopiesQuery = "UPDATE Books SET CopiesAvailable = CopiesAvailable + 1 WHERE Code = @BookCode";

                    using (SqlCommand updateCopiesCommand = new SqlCommand(updateCopiesQuery, connection))
                    {
                        updateCopiesCommand.Parameters.AddWithValue("@BookCode", bookCode);
                        updateCopiesCommand.ExecuteNonQuery();
                    }
                }

                LoadBooksComboBox();
            }
        }

        private void buttonSearchBook_Click(object sender, EventArgs e)
        {
            if (comboBoxBooks.SelectedItem == null)
            {
                MessageBox.Show("Molimo odaberite knjigu koju tražite.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string selectedBookDetails = comboBoxBooks.SelectedItem.ToString();
                string bookCode = selectedBookDetails.Substring(selectedBookDetails.LastIndexOf("Code:") + 6).Trim();

                string message = "Studenti koji su posudili tu knjigu:" + Environment.NewLine;
                bool found = false;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Students.Name, BorrowedBooks.ReturnDate " +
                                   "FROM BorrowedBooks " +
                                   "JOIN Students ON BorrowedBooks.StudentId = Students.Id " +
                                   "WHERE BorrowedBooks.BookCode = @BookCode";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BookCode", bookCode);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                found = true;
                                string studentName = reader["Name"].ToString();
                                DateTime returnDate = Convert.ToDateTime(reader["ReturnDate"]);

                                if (returnDate < DateTime.Today)
                                {
                                    message += $"{studentName} (Datum povratka: {returnDate.ToString("dd/MM/yyyy")}) - Datum povratka je istekao." + Environment.NewLine;
                                }
                                else
                                {
                                    message += $"{studentName} (Datum povratka: {returnDate.ToString("dd/MM/yyyy")})" + Environment.NewLine;
                                }
                            }
                        }
                    }
                }

                if (!found)
                {
                    message = "Nema uèenika koji su posudili tu knjigu.";
                }

                MessageBox.Show(message, "Posuðeno", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonRemoveBook_Click(object sender, EventArgs e)
        {
            if (comboBoxBooks.SelectedItem == null)
            {
                MessageBox.Show("Odaberi knjigu.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedBookDetails = comboBoxBooks.SelectedItem.ToString();
            string bookCode = selectedBookDetails.Substring(selectedBookDetails.LastIndexOf("Code:") + 6).Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkBorrowedQuery = "SELECT COUNT(*) FROM BorrowedBooks WHERE BookCode = @BookCode";

                using (SqlCommand checkBorrowedCommand = new SqlCommand(checkBorrowedQuery, connection))
                {
                    checkBorrowedCommand.Parameters.AddWithValue("@BookCode", bookCode);

                    int count = (int)checkBorrowedCommand.ExecuteScalar();

                    if (count > 0)
                    {
                        MessageBox.Show("Ova knjiga se ne može obrisati jer je posuðena.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                DialogResult result = MessageBox.Show("Da li sigurno želite obrisati ovu knjigu?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    string removeBookQuery = "DELETE FROM Books WHERE Code = @BookCode";

                    using (SqlCommand removeBookCommand = new SqlCommand(removeBookQuery, connection))
                    {
                        removeBookCommand.Parameters.AddWithValue("@BookCode", bookCode);
                        removeBookCommand.ExecuteNonQuery();
                    }

                    comboBoxBooks.Items.Remove(comboBoxBooks.SelectedItem);

                    MessageBox.Show("Knjiga je uspješno obrisana.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            LoadBooksComboBox();
        }

        private void buttonAddStudent_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.ToString() == "")
            {
                MessageBox.Show("Polje ne smije biti prazno");
            }
            else
            {
                string name = textBox1.Text;
                string id = CodeGeneratorStudent.GenerateUniqueCode(library.GetStudents().Select(b => b.Id).ToList());

                library.AddStudent(name, id);


                LoadStudentsComboBox();
            }
        }




        private void buttonRemoveStudent_Click(object sender, EventArgs e)
        {
            if (comboBoxStudents.SelectedItem == null)
            {
                MessageBox.Show("Odaberite studenta.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string selectedStudentName = comboBoxStudents.SelectedItem.ToString();
                string[] studentDetails = selectedStudentName.Split('-');
                string studentName = studentDetails[0].Trim();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM BorrowedBooks " +
                                        "JOIN Students ON BorrowedBooks.StudentId = Students.Id " +
                                        "WHERE Students.Name = @Name";

                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Name", studentName);

                        int count = (int)checkCommand.ExecuteScalar();

                        if (count > 0)
                        {
                            MessageBox.Show($"{studentName} ne može biti obrisan jer posuðuje knjige.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    string removeQuery = "DELETE FROM Students WHERE Name = @Name";

                    using (SqlCommand removeCommand = new SqlCommand(removeQuery, connection))
                    {
                        removeCommand.Parameters.AddWithValue("@Name", studentName);
                        removeCommand.ExecuteNonQuery();

                        MessageBox.Show($"{studentName} je uspješno obrisan.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                       LoadStudentsComboBox();
                    }
                }
            }

        }



        private void Form1_Click(object sender, EventArgs e)
        {

        }

        private void ButtonInfo_Click(object sender, EventArgs e)
        {
            if (comboBoxStudents.SelectedItem == null)
            {
                MessageBox.Show("Odaberite studenta.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string selectedStudentName = comboBoxStudents.SelectedItem.ToString();
                string[] studentDetails = selectedStudentName.Split('-');
                string studentName = studentDetails[0].Trim();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string selectQuery = "SELECT Books.Name, Books.Author, BorrowedBooks.BorrowDate, BorrowedBooks.ReturnDate " +
                                         "FROM BorrowedBooks " +
                                         "JOIN Students ON BorrowedBooks.StudentId = Students.Id " +
                                         "JOIN Books ON BorrowedBooks.BookCode = Books.Code " +
                                         "WHERE Students.Name = @Name";

                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@Name", studentName);

                        StringBuilder borrowedBooks = new StringBuilder();

                        using (SqlDataReader reader = selectCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime returnDate = Convert.ToDateTime(reader["ReturnDate"]);
                                string status = returnDate < DateTime.Now ? " (Overdue)" : "";
                                borrowedBooks.AppendLine($"{reader["Name"]} by {reader["Author"]} - Datum vraèanja: {returnDate.ToShortDateString()}{status}");
                            }
                        }

                        MessageBox.Show($"Posuðene knjige:\n{borrowedBooks}", "Posuðene knjige od strane " + selectedStudentName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void buttonBorrowBook_Click(object sender, EventArgs e)
        {
            if (comboBoxStudents.SelectedItem == null || comboBoxBooks.SelectedItem == null)
            {
                MessageBox.Show("Odaberite knjigu i studenta.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedStudentName = comboBoxStudents.SelectedItem.ToString();
            string[] studentDetails = selectedStudentName.Split('-');
            string studentName = studentDetails[0].Trim();

            string selectedBookDetails = comboBoxBooks.SelectedItem.ToString();
            string bookCode = selectedBookDetails.Substring(selectedBookDetails.LastIndexOf("Code:") + 6).Trim();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkQuery = "SELECT COUNT(*) FROM BorrowedBooks " +
                                    "JOIN Students ON BorrowedBooks.StudentId = Students.Id " +
                                    "WHERE BorrowedBooks.BookCode = @BookCode AND Students.Name = @StudentName";

                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@BookCode", bookCode);
                    checkCommand.Parameters.AddWithValue("@StudentName", studentName);

                    int count = (int)checkCommand.ExecuteScalar();

                    if (count > 0)
                    {
                        MessageBox.Show($"{studentName} veæ posuðuje knjigu sa šifrom {bookCode}.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                string updateCopiesQuery = "UPDATE Books SET CopiesAvailable = CopiesAvailable - 1 WHERE Code = @BookCode";

                using (SqlCommand updateCopiesCommand = new SqlCommand(updateCopiesQuery, connection))
                {
                    updateCopiesCommand.Parameters.AddWithValue("@BookCode", bookCode);
                    updateCopiesCommand.ExecuteNonQuery();
                }

                string borrowQuery = "INSERT INTO BorrowedBooks (StudentId, BookCode, BorrowDate, ReturnDate) " +
                                     "VALUES ((SELECT Id FROM Students WHERE Name = @StudentName), @BookCode, @BorrowDate, @ReturnDate)";

                using (SqlCommand borrowCommand = new SqlCommand(borrowQuery, connection))
                {
                    borrowCommand.Parameters.AddWithValue("@StudentName", studentName);
                    borrowCommand.Parameters.AddWithValue("@BookCode", bookCode);
                    borrowCommand.Parameters.AddWithValue("@BorrowDate", DateTime.Now);
                    borrowCommand.Parameters.AddWithValue("@ReturnDate", DateTime.Now.AddDays(30));

                    borrowCommand.ExecuteNonQuery();

                    MessageBox.Show($"{studentName} je uspješno posudio knjigu {selectedBookDetails}.", "Uspjeh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            LoadBooksComboBox();
        }

        public class CodeGenerator
        {
            private static readonly Random random = new Random();
            private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            public static string GenerateUniqueCode(List<string> existingCodes)
            {
                string code;
                do
                {
                    code = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
                } while (existingCodes.Contains(code));
                return code;
            }
        }

        public static class CodeGeneratorStudent
        {
            private static readonly Random random = new Random();
            private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            public static string GenerateUniqueCode(List<string> existingCodesStudents)
            {
                string code;
                do
                {
                    code = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
                } while (existingCodesStudents.Contains(code));
                return code;
            }
        }



        public class Book
        {
            private string code;
            private string name;
            private string author;
            private int copiesAvailable;

            public string Code { get { return code; } }
            public string Name { get { return name; } }
            public string Author { get { return author; } }
            public int CopiesAvailable { get { return copiesAvailable; } }

            public Book(string code, string name, string author, int copiesAvailable)
            {
                this.code = code;
                this.name = name;
                this.author = author;
                this.copiesAvailable = copiesAvailable;
            }

        }


        static string FetchText()
        {

            string filePath = @"D:\37723fc59c3368f4a26bed6c3e9707cc155f6ffe6299cd0551d6bac7a3e42406.txt";

            string initialContent = "false";

            if (!File.Exists(filePath))
            {
                try
                {
                    File.WriteAllText(filePath, initialContent);

                }
                catch
                {

                }
            }
            else
            {
                string currentContent = File.ReadAllText(filePath);
                try
                {
                }
                catch
                {
                    File.WriteAllText(filePath, "true");

                }
                

                return currentContent;
            }

            return initialContent;
        }




        public class Student
        {
            public string Id { get; private set; }
            public string Name { get; private set; }
            public List<Book> BorrowedBooks { get; private set; }

            public Student(string name, string id)
            {
                Id = id;
                Name = name;
                BorrowedBooks = new List<Book>();
            }

        }

        public class Library
        {
            private List<Book> books;
            private List<Student> students;
            private string connectionString = "Data Source=DESKTOP-76L4HIG;Initial Catalog=KnjiznicaV3;Persist Security Info=True;User ID=Karlo;Password=***********;Pooling=False; TrustServerCertificate=True; Trusted_Connection=True";
            private string ini = FetchText();


            public Library()
            {
                books = new List<Book>
            {

                new Book("B001", "The Great Gatsby", "F. Scott Fitzgerald", 5),
                new Book("B002", "To Kill a Mockingbird", "Harper Lee", 5),
                new Book("B003", "1984", "George Orwell", 5),
                new Book("B004", "The Catcher in the Rye", "J.D. Salinger", 5)

            };

                students = new List<Student>
            {

                new Student("Ivor Trstenjak" , "ABCD"),
                new Student("Jakov Biškup" , "DBAC"),
                new Student("Noel Špoljariæ" , "DFFE"),
                new Student("Karlo Preložnjak" , "AAAD"),
                new Student("Andro Suvajac" , "DNFD")

            };

                EnsureDatabaseCreated();
            }
      

            public List<Book> GetBooks()
            {
                return books;
            }

            public List<Student> GetStudents()
            {
                return students;
            }


            public void AddBook(string code, string name, string author, int copiesAvailable)
            {
                Book newBook = new Book(code, name, author, copiesAvailable);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string insertQuery = "INSERT INTO Books (Code, Name, Author, CopiesAvailable) VALUES (@Code, @Name, @Author, @CopiesAvailable)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Code", code);
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Author", author);
                        command.Parameters.AddWithValue("@CopiesAvailable", copiesAvailable);

                        command.ExecuteNonQuery();
                    }
                }

            }

            public void AddStudent(string name, string id)
            {
                Student newStudent = new Student(name, id);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string insertQuery = "INSERT INTO Students (Name, Id) VALUES (@Name, @Id)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Id", id);

                        command.ExecuteNonQuery();
                    }
                }
            }

            private void EnsureDatabaseCreated()
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string createDatabaseQuery = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'KnjiznicaV3') " +
                                                "CREATE DATABASE KnjiznicaV3";
                    using (SqlCommand command = new SqlCommand(createDatabaseQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.ChangeDatabase("KnjiznicaV3");

                    string createBooksTableQuery = "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Books') " +
                                                  "CREATE TABLE Books (Code NVARCHAR(50), Name NVARCHAR(100), Author NVARCHAR(100), CopiesAvailable INT)";
                    using (SqlCommand command = new SqlCommand(createBooksTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string createStudentsTableQuery = "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Students') " +
                                                     "CREATE TABLE Students (Name NVARCHAR(100), Id NVARCHAR(50))";
                    using (SqlCommand command = new SqlCommand(createStudentsTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string createBorrowedBooksTableQuery = "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BorrowedBooks') " +
                                                           "CREATE TABLE BorrowedBooks (StudentId NVARCHAR(50), BookCode NVARCHAR(50), BorrowDate DATETIME, ReturnDate DATETIME)";
                    using (SqlCommand command = new SqlCommand(createBorrowedBooksTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }


                    if (ini == "false")
                    {
                        string insertExistingBooksQuery = "INSERT INTO Books (Code, Name, Author, CopiesAvailable) VALUES (@Code, @Name, @Author, @CopiesAvailable)";
                        foreach (Book book in GetBooks())
                        {
                            using (SqlCommand command = new SqlCommand(insertExistingBooksQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Code", book.Code);
                                command.Parameters.AddWithValue("@Name", book.Name);
                                command.Parameters.AddWithValue("@Author", book.Author);
                                command.Parameters.AddWithValue("@CopiesAvailable", book.CopiesAvailable);

                                command.ExecuteNonQuery();
                            }
                        }

                        string insertExistingStudentsQuery = "INSERT INTO Students (Name, Id) VALUES (@Name, @Id)";
                        foreach (Student student in GetStudents())
                        {
                            using (SqlCommand command = new SqlCommand(insertExistingStudentsQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Name", student.Name);
                                command.Parameters.AddWithValue("@Id", student.Id);

                                command.ExecuteNonQuery();
                            }
                        }




                    }
                    else
                    {
                        MessageBox.Show("The database is already initialized.");
                    }

                }
            }

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            string filePath = @"D:\37723fc59c3368f4a26bed6c3e9707cc155f6ffe6299cd0551d6bac7a3e42406.txt";
            File.WriteAllText(filePath, "true");
        }

        private void LoadBooksComboBox()
        {
            comboBoxBooks.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Books";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string bookCode = reader["Code"].ToString();
                            string bookName = reader["Name"].ToString();
                            string bookAuthor = reader["Author"].ToString();
                            string bookCopies = reader["CopiesAvailable"].ToString();
                            comboBoxBooks.Items.Add($"{bookName} by {bookAuthor} - Copies Available - {bookCopies} - Code: {bookCode}");
                        }
                    }
                }
            }
        }

        private void LoadStudentsComboBox()
        {
            comboBoxStudents.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Students";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string studentId = reader["Id"].ToString();
                            string studentName = reader["Name"].ToString();
                            comboBoxStudents.Items.Add($"{studentName} - ID: {studentId}");
                        }
                    }
                }
            }
        }
    }
}

