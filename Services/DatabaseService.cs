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

        // Aktualizuje temat fiszki w bazie na "wybrany"
        public static void MarkTopicAsSelected(int topicId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var cmd1 = new MySqlCommand("UPDATE temat SET czy_wybrany = 1 WHERE id = @topicId", connection);
                cmd1.Parameters.AddWithValue("@topicId", topicId);
                cmd1.ExecuteNonQuery();

                // B) Ustawienie jutrzejszej daty w tabeli tlumaczenie.
                // Łączymy tabele tlumaczenie i slowko, aby zaktualizować tylko słówka z tego tematu (i tylko angielskie id=1).
                var cmd2 = new MySqlCommand(@"
            UPDATE tlumaczenie t 
            JOIN slowko s ON t.id_slowka = s.id 
            SET t.data_nastepnej_powtorki = @dzisiaj
            WHERE s.id_tematu = @topicId AND t.id_jezyka = 1", connection);

                // DateTime.Today ustawia czas na 00:00:00, AddDays(1) to jutro
                cmd2.Parameters.AddWithValue("@dzisiaj", DateTime.Today);
                cmd2.Parameters.AddWithValue("@topicId", topicId);
                cmd2.ExecuteNonQuery();
            }
        }

        // Pobiera liczby zaplanowanych powtórek na dany miesiąc i rok
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
        public static int GetReviewCountForToday() // dla funkcji z okienkiem czy można otworzyć menu Flashcards czy mamy jakieś fiszki na dzisiaj
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                // Funkcja CURDATE() w MySQL zwraca dzisiejszą datę.
                // MOŻNA: Używamy <= aby złapać też zaległe powtórki z wczoraj czy przedwczoraj.
                var cmd = new MySqlCommand(@"
            SELECT COUNT(*) 
            FROM tlumaczenie 
            WHERE DATE(data_nastepnej_powtorki) = CURDATE() 
              AND id_jezyka = 1", connection);

                // ExecuteScalar zwraca pierwszy z brzegu wynik jako obiekt, więc bezpiecznie konwertujemy
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        public static List<FlashcardModel> GetFlashcardsForToday()
        {
            var flashcards = new List<FlashcardModel>();

            using (var connection = GetConnection())
            {
                connection.Open();

                // Łączymy tlumaczenie -> slowko -> temat -> dzial
                string query = @"
            SELECT 
                t.id AS TranslationId,
                dz.nazwa AS CategoryName,
                tem.nazwa AS TopicName,
                s.grafika AS PolImageName,
                t.grafika AS EngImageName,
                t.wspolczynnik_latwosci AS EasinessFactor,
                t.odstep_powtorek AS IntervalDays,
                t.liczba_poprawnych_powtorek AS Repetitions
            FROM tlumaczenie t
            JOIN slowko s ON t.id_slowka = s.id
            JOIN temat tem ON s.id_tematu = tem.id
            JOIN dzial dz ON tem.id_dzialu = dz.id
            WHERE DATE(t.data_nastepnej_powtorki) <= CURDATE()
              AND t.id_jezyka = 1";

                var cmd = new MySqlCommand(query, connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int efOrdinal = reader.GetOrdinal("EasinessFactor");
                        int intervalOrdinal = reader.GetOrdinal("IntervalDays");
                        int repOrdinal = reader.GetOrdinal("Repetitions");

                        flashcards.Add(new FlashcardModel
                        {
                            TranslationId = reader.GetInt32("TranslationId"),
                            CategoryName = reader.GetString("CategoryName"),
                            TopicName = reader.GetString("TopicName"),
                            PolImageName = reader.GetString("PolImageName"),
                            EngImageName = reader.GetString("EngImageName"),
                            EasinessFactor = reader.IsDBNull(efOrdinal) ? 2.5 : reader.GetDouble(efOrdinal),
                            Interval = reader.IsDBNull(intervalOrdinal) ? 0 : reader.GetInt32(intervalOrdinal),
                            Repetitions = reader.IsDBNull(repOrdinal) ? 0 : reader.GetInt32(repOrdinal)
                        });
                    }
                }
            }

            return flashcards;
        }

        public static void UpdateFlashcardProgress(FlashcardModel card, bool isCorrect)
        {
            int n = card.Repetitions;
            int interval = card.Interval;
            double ef = card.EasinessFactor;

            if (isCorrect)
            {
                n++; // 1. Zwiększamy serię poprawnych odpowiedzi

                if (n == 1) // 2. Obliczamy nowy odstęp używając dotychczasowego EF
                {
                    interval = 1;
                }
                else if (n == 2)
                {
                    interval = 6;
                }
                else
                {
                    interval = (int)Math.Round(interval * ef, MidpointRounding.AwayFromZero);
                }

                ef = Math.Round(ef + 0.10, 2); // 3. Aktualizujemy EF dla q = 5 (+0.10)
            }
            else
            {
                // Błędna odpowiedź (q = 2): reset serii i odstępu, obniżenie EF z limitem min. 1.3
                n = 0;
                interval = 1;
                ef = Math.Round(Math.Max(1.3, ef - 0.32), 2);
            }

            DateTime today = DateTime.Today;
            DateTime nextReviewDate = today.AddDays(interval);

            using (var connection = GetConnection())
            {
                connection.Open();

                string query = @"
            UPDATE tlumaczenie 
            SET 
                data_ostatniej_powtorki = @lastReview,
                data_nastepnej_powtorki = @nextReview,
                wspolczynnik_latwosci = @ef,
                odstep_powtorek = @interval,
                liczba_poprawnych_powtorek = @repetitions
            WHERE id = @id";

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@lastReview", today);
                cmd.Parameters.AddWithValue("@nextReview", nextReviewDate);
                cmd.Parameters.AddWithValue("@ef", ef);
                cmd.Parameters.AddWithValue("@interval", interval);
                cmd.Parameters.AddWithValue("@repetitions", n);
                cmd.Parameters.AddWithValue("@id", card.TranslationId);

                cmd.ExecuteNonQuery();
            }
        }
    }
}