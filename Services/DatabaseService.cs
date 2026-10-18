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

                // A) Zmiana statusu na "wybrany" w tabeli temat
                var cmd1 = new MySqlCommand("UPDATE temat SET czy_wybrany = 1 WHERE id = @topicId", connection);
                cmd1.Parameters.AddWithValue("@topicId", topicId);
                cmd1.ExecuteNonQuery();

                // B) Ustawienie jutrzejszej daty w tabeli tlumaczenie.
                // Łączymy tabele tlumaczenie i slowko, aby zaktualizować tylko słówka z tego tematu (i tylko angielskie id=1).
                var cmd2 = new MySqlCommand(@"
            UPDATE tlumaczenie t 
            JOIN slowko s ON t.id_slowka = s.id 
            SET t.data_nastepnej_powtorki = @jutro 
            WHERE s.id_tematu = @topicId AND t.id_jezyka = 1", connection);

                // DateTime.Today ustawia czas na 00:00:00, AddDays(1) to jutro
                cmd2.Parameters.AddWithValue("@jutro", DateTime.Today.AddDays(1));
                cmd2.Parameters.AddWithValue("@topicId", topicId);
                cmd2.ExecuteNonQuery();
            }
        }

        // 2. NOWA METODA: Pobiera liczby zaplanowanych powtórek na dany miesiąc i rok
        public static Dictionary<DateTime, int> GetReviewCountsForMonth(int year, int month)
        {
            var result = new Dictionary<DateTime, int>();
            using (var connection = GetConnection())
            {
                connection.Open();

                // Zliczamy słówka grupując je po dacie powtórki
                var cmd = new MySqlCommand(@"
            SELECT DATE(data_nastepnej_powtorki) as review_date, COUNT(*) as count 
            FROM tlumaczenie 
            WHERE YEAR(data_nastepnej_powtorki) = @year 
              AND MONTH(data_nastepnej_powtorki) = @month
              AND id_jezyka = 1
            GROUP BY DATE(data_nastepnej_powtorki)", connection);

                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);

                using (var reader = cmd.ExecuteReader())
                {
                    // 1. MUSI tu być pętla while, która przesuwa "kursor" bazy na kolejny wiersz!
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal("review_date")))
                        {
                            // 2. Bezpieczna konwersja (radzi sobie ze specyficznymi typami MySqlDateTime)
                            DateTime date = Convert.ToDateTime(reader["review_date"]);

                            // 3. Bezpieczna konwersja BIGINT z COUNT(*) na zwykłego inta
                            int count = Convert.ToInt32(reader["count"]);

                            result[date] = count;
                        }
                    }
                }
            }
            return result;
        }
        public static int GetReviewCountForToday()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                // Funkcja CURDATE() w MySQL zwraca dzisiejszą datę.
                // Używamy <= aby złapać też zaległe powtórki z wczoraj czy przedwczoraj.
                var cmd = new MySqlCommand(@"
            SELECT COUNT(*) 
            FROM tlumaczenie 
            WHERE DATE(data_nastepnej_powtorki) <= CURDATE() 
              AND id_jezyka = 1", connection);

                // ExecuteScalar zwraca pierwszy z brzegu wynik jako obiekt, więc bezpiecznie konwertujemy
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}