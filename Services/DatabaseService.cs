using System;
using DotNetEnv;
using MySql.Data.MySqlClient;
using LearnLanguages.Models;
using System.Collections.Generic;

namespace LearnLanguages.Services
{
    public static class DatabaseService
    {
        public static MySqlConnection GetConnection()
        {
            // Ładowanie pliku .env umieszczonego w głównym folderze
            Env.Load();

            string server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            string port = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
            string database = Environment.GetEnvironmentVariable("DB_NAME");
            string user = Environment.GetEnvironmentVariable("DB_USER");
            string password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

            string connectionString = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};";

            return new MySqlConnection(connectionString);
        }

        // Pobiera wszystkie działy z bazy
        public static List<Department> GetDepartments()
        {
            var departments = new List<Department>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT id, nazwa FROM dzial", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        departments.Add(new Department
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("nazwa")
                        });
                    }
                }
            }
            return departments;
        }

        // Pobiera tematy dla konkretnego działu
        public static List<Topic> GetTopicsByDepartment(int departmentId)
        {
            var topics = new List<Topic>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT id, id_dzialu, nazwa, czy_wybrany FROM temat WHERE id_dzialu = @deptId", connection);
                command.Parameters.AddWithValue("@deptId", departmentId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        topics.Add(new Topic
                        {
                            Id = reader.GetInt32("id"),
                            DepartmentId = reader.GetInt32("id_dzialu"),
                            Name = reader.GetString("nazwa"),
                            // MySQL przechowuje boolean jako TINYINT(1), GetBoolean automatycznie to zmapuje
                            IsSelected = reader.GetBoolean("czy_wybrany")
                        });
                    }
                }
            }
            return topics;
        }

        // Aktualizuje status tematu na "wybrany"
        public static void MarkTopicAsSelected(int topicId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("UPDATE temat SET czy_wybrany = 1 WHERE id = @topicId", connection);
                command.Parameters.AddWithValue("@topicId", topicId);
                command.ExecuteNonQuery();
            }
        }
    }
}