using DotNetEnv;
using LearnLanguages.Models;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System;
using System.Collections.Generic;

namespace LearnLanguages.Services
{
    public class MySqlDatabaseService : IDatabaseService
    {
        private MySqlConnection GetConnection()
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
        public bool TestConnection(out string errorMessage)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    errorMessage = string.Empty;
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
        public ProfileModel CurrentProfile { get; set; }

        private int CurrentProfileId =>
            CurrentProfile?.Id ?? throw new InvalidOperationException("Nie wybrano profilu użytkownika.");

        public List<ProfileModel> GetProfiles()
        {
            var profiles = new List<ProfileModel>();

            using (var connection = GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT id, nazwa FROM profil ORDER BY id ASC", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        profiles.Add(new ProfileModel
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("nazwa")
                        });
                    }
                }
            }

            return profiles;
        }

        public void CreateProfile(string name)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand("INSERT INTO profil (nazwa) VALUES (@name)", connection);
                cmd.Parameters.AddWithValue("@name", name.Trim());
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteProfile(int profileId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand("DELETE FROM profil WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("@id", profileId);
                cmd.ExecuteNonQuery();
            }
        }

        // Pobiera wszystkie działy z bazy
        public List<Department> GetDepartments()
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
        public List<Topic> GetTopicsByDepartment(int departmentId)
        {
            var topics = new List<Topic>();

            using (var connection = GetConnection())
            {
                connection.Open();

                string query = @"
            SELECT 
                tem.id AS Id,
                tem.id_dzialu AS DepartmentId,
                tem.nazwa AS Name,
                COALESCE(pt.czy_wybrany, 0) AS IsSelected,
                COUNT(t.id) AS WordsCount
            FROM temat tem
            LEFT JOIN profil_temat pt ON pt.id_tematu = tem.id AND pt.id_profilu = @profileId
            LEFT JOIN slowko s ON s.id_tematu = tem.id
            LEFT JOIN tlumaczenie t ON t.id_slowka = s.id AND t.id_jezyka = 1
            WHERE tem.id_dzialu = @departmentId
            GROUP BY tem.id, tem.id_dzialu, tem.nazwa, pt.czy_wybrany
            ORDER BY tem.nazwa ASC";

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                cmd.Parameters.AddWithValue("@departmentId", departmentId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        topics.Add(new Topic
                        {
                            Id = reader.GetInt32("Id"),
                            DepartmentId = reader.GetInt32("DepartmentId"),
                            Name = reader.GetString("Name"),
                            IsSelected = reader.GetBoolean("IsSelected"),
                            WordsCount = Convert.ToInt32(reader["WordsCount"])
                        });
                    }
                }
            }

            return topics;
        }

        // Aktualizuje temat fiszki w bazie na "wybrany"
        public void MarkTopicAsSelected(int topicId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                int profileId = CurrentProfileId;

                var cmd1 = new MySqlCommand(@"
            INSERT INTO profil_temat (id_profilu, id_tematu, czy_wybrany)
            VALUES (@profileId, @topicId, 1)
            ON DUPLICATE KEY UPDATE czy_wybrany = 1", connection);

                cmd1.Parameters.AddWithValue("@profileId", profileId);
                cmd1.Parameters.AddWithValue("@topicId", topicId);
                cmd1.ExecuteNonQuery();

                var cmd2 = new MySqlCommand(@"
            INSERT IGNORE INTO profil_postep 
                (id_profilu, id_tlumaczenia, wspolczynnik_latwosci, odstep_powtorek, liczba_poprawnych_powtorek, data_nastepnej_powtorki)
            SELECT @profileId, t.id, 2.5, 0, 0, @dzisiaj
            FROM tlumaczenie t
            JOIN slowko s ON t.id_slowka = s.id
            WHERE s.id_tematu = @topicId AND t.id_jezyka = 1", connection);

                cmd2.Parameters.AddWithValue("@profileId", profileId);
                cmd2.Parameters.AddWithValue("@dzisiaj", DateTime.Today);
                cmd2.Parameters.AddWithValue("@topicId", topicId);
                cmd2.ExecuteNonQuery();
            }
        }
        public void UnmarkTopicAsSelected(int topicId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand(@"
            UPDATE profil_temat 
            SET czy_wybrany = 0 
            WHERE id_profilu = @profileId AND id_tematu = @topicId", connection);

                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                cmd.Parameters.AddWithValue("@topicId", topicId);
                cmd.ExecuteNonQuery();
            }
        }

        // Pobiera liczby zaplanowanych powtórek na dany miesiąc i rok
        public Dictionary<DateTime, int> GetReviewCountsForMonth(int year, int month)
        {
            var result = new Dictionary<DateTime, int>();
            using (var connection = GetConnection())
            {
                connection.Open();

                // Zliczamy słówka grupując je po dacie powtórki
                var cmd = new MySqlCommand(@"
            SELECT DATE(pp.data_nastepnej_powtorki) as review_date, COUNT(*) as count 
            FROM profil_postep pp
            JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
            JOIN slowko s ON t.id_slowka = s.id
            JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
            WHERE pp.id_profilu = @profileId
              AND pt.czy_wybrany = 1
              AND YEAR(pp.data_nastepnej_powtorki) = @year 
              AND MONTH(pp.data_nastepnej_powtorki) = @month
              AND DATE(pp.data_nastepnej_powtorki) >= CURDATE()
              AND t.id_jezyka = 1
            GROUP BY DATE(pp.data_nastepnej_powtorki)", connection);

                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);

                using (var reader = cmd.ExecuteReader())
                {
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
        public int GetOverdueReviewCount()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var cmd = new MySqlCommand(@"
            SELECT COUNT(*) 
            FROM profil_postep pp
            JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
            JOIN slowko s ON t.id_slowka = s.id
            JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
            WHERE pp.id_profilu = @profileId
              AND pt.czy_wybrany = 1
              AND DATE(pp.data_nastepnej_powtorki) < CURDATE() 
              AND t.id_jezyka = 1", connection);

                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        public int GetReviewCountForToday() // dla funkcji z okienkiem czy można otworzyć menu Flashcards czy mamy jakieś fiszki na dzisiaj
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var cmd = new MySqlCommand(@"
            SELECT COUNT(*) 
            FROM profil_postep pp
            JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
            JOIN slowko s ON t.id_slowka = s.id
            JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
            WHERE pp.id_profilu = @profileId
              AND pt.czy_wybrany = 1
              AND DATE(pp.data_nastepnej_powtorki) <= CURDATE() 
              AND t.id_jezyka = 1", connection);

                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        public List<TopicReviewSummary> GetTopicReviewSummariesForToday()
        {
            var summaries = new List<TopicReviewSummary>();

            using (var connection = GetConnection())
            {
                connection.Open();

                string query = @"
            SELECT 
                tem.id AS TopicId,
                tem.nazwa AS TopicName,
                dz.nazwa AS CategoryName,
                SUM(CASE WHEN DATE(pp.data_nastepnej_powtorki) = CURDATE() THEN 1 ELSE 0 END) AS TodayCount,
                SUM(CASE WHEN DATE(pp.data_nastepnej_powtorki) < CURDATE() THEN 1 ELSE 0 END) AS OverdueCount
            FROM profil_postep pp
            JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
            JOIN slowko s ON t.id_slowka = s.id
            JOIN temat tem ON s.id_tematu = tem.id
            JOIN dzial dz ON tem.id_dzialu = dz.id
            JOIN profil_temat pt ON pt.id_tematu = tem.id AND pt.id_profilu = pp.id_profilu
            WHERE pp.id_profilu = @profileId
                AND pt.czy_wybrany = 1
                AND DATE(pp.data_nastepnej_powtorki) <= CURDATE()
                AND t.id_jezyka = 1
            GROUP BY tem.id, tem.nazwa, dz.nazwa
            ORDER BY OverdueCount DESC, dz.nazwa ASC, tem.nazwa ASC";

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        summaries.Add(new TopicReviewSummary
                        {
                            TopicId = reader.GetInt32("TopicId"),
                            TopicName = reader.GetString("TopicName"),
                            CategoryName = reader.GetString("CategoryName"),
                            TodayCount = Convert.ToInt32(reader["TodayCount"]),
                            OverdueCount = Convert.ToInt32(reader["OverdueCount"])
                        });
                    }
                }
            }

            return summaries;
        }
        public int GetUpcomingReviewsCount(int days = 3)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var cmd = new MySqlCommand(@"
            SELECT COUNT(*) 
            FROM profil_postep pp
            JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
            JOIN slowko s ON t.id_slowka = s.id
            JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
            WHERE pp.id_profilu = @profileId
              AND pt.czy_wybrany = 1
              AND DATE(pp.data_nastepnej_powtorki) <= DATE_ADD(CURDATE(), INTERVAL @daysOffset DAY)
              AND t.id_jezyka = 1", connection);

                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                cmd.Parameters.AddWithValue("@daysOffset", days - 1);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool HasUnselectedTopics()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var cmd = new MySqlCommand(@"
            SELECT COUNT(*)
            FROM temat tem
            LEFT JOIN profil_temat pt ON pt.id_tematu = tem.id AND pt.id_profilu = @profileId
            WHERE COALESCE(pt.czy_wybrany, 0) = 0", connection);

                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
        public List<FlashcardModel> GetFlashcardsForToday(int? topicId = null, bool onlyOverdue = false)
        {
            var flashcards = new List<FlashcardModel>();

            using (var connection = GetConnection())
            {
                connection.Open();

                string dateCondition = onlyOverdue
                    ? "DATE(pp.data_nastepnej_powtorki) < CURDATE()"
                    : "DATE(pp.data_nastepnej_powtorki) <= CURDATE()";

                string topicCondition = topicId.HasValue
                    ? "AND tem.id = @topicId"
                    : "";

                string query = $@"
            SELECT 
                t.id AS TranslationId,
                tem.id AS TopicId,
                dz.nazwa AS CategoryName,
                tem.nazwa AS TopicName,
                s.grafika AS PolImageName,
                t.grafika AS EngImageName,
                pp.wspolczynnik_latwosci AS EasinessFactor,
                pp.odstep_powtorek AS IntervalDays,
                pp.liczba_poprawnych_powtorek AS Repetitions,
                pp.data_nastepnej_powtorki AS NextReviewDate
            FROM profil_postep pp
            JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
            JOIN slowko s ON t.id_slowka = s.id
            JOIN temat tem ON s.id_tematu = tem.id
            JOIN dzial dz ON tem.id_dzialu = dz.id
            JOIN profil_temat pt ON pt.id_tematu = tem.id AND pt.id_profilu = pp.id_profilu
            WHERE pp.id_profilu = @profileId
              AND pt.czy_wybrany = 1
              AND {dateCondition}
              AND t.id_jezyka = 1
              {topicCondition}
            ORDER BY pp.data_nastepnej_powtorki ASC";

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                if (topicId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@topicId", topicId.Value);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int efOrdinal = reader.GetOrdinal("EasinessFactor");
                        int intervalOrdinal = reader.GetOrdinal("IntervalDays");
                        int repOrdinal = reader.GetOrdinal("Repetitions");
                        int nextDateOrdinal = reader.GetOrdinal("NextReviewDate");

                        flashcards.Add(new FlashcardModel
                        {
                            TranslationId = reader.GetInt32("TranslationId"),
                            TopicId = reader.GetInt32("TopicId"),
                            CategoryName = reader.GetString("CategoryName"),
                            TopicName = reader.GetString("TopicName"),
                            PolImageName = reader.GetString("PolImageName"),
                            EngImageName = reader.GetString("EngImageName"),
                            EasinessFactor = reader.IsDBNull(efOrdinal) ? 2.5 : reader.GetDouble(efOrdinal),
                            Interval = reader.IsDBNull(intervalOrdinal) ? 0 : reader.GetInt32(intervalOrdinal),
                            Repetitions = reader.IsDBNull(repOrdinal) ? 0 : reader.GetInt32(repOrdinal),
                            NextReviewDate = reader.IsDBNull(nextDateOrdinal) ? DateTime.Today : reader.GetDateTime(nextDateOrdinal)
                        });
                    }
                }
            }
            return flashcards;
        }
        public void UpdateFlashcardProgress(FlashcardModel card, bool isCorrect)
        {
            int n = card.Repetitions;
            int interval = card.Interval;
            double ef = card.EasinessFactor;
            DateTime today = DateTime.Today;

            // Liczba dni spóźnienia (0, jeśli fiszka jest powtarzana w terminie)
            int overdueDays = Math.Max(0, (today - card.NextReviewDate.Date).Days);

            if (isCorrect)
            {
                n++; // 1. Zwiększamy serię poprawnych odpowiedzi

                if (n == 1) // 2. Obliczamy nowy odstęp używając dotychczasowego EF
                {
                    interval = 1;
                }
                else if (n == 2)
                {
                    interval = (int)Math.Round(6 + (overdueDays / 2.0), MidpointRounding.AwayFromZero);
                }
                else
                {
                    interval = (int)Math.Round((interval + (overdueDays / 2.0)) * ef, MidpointRounding.AwayFromZero);
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

            DateTime nextReviewDate = today.AddDays(interval);

            using (var connection = GetConnection())
            {
                connection.Open();

                string query = @"
            UPDATE profil_postep 
            SET 
                data_nastepnej_powtorki = @nextReview,
                wspolczynnik_latwosci = @ef,
                odstep_powtorek = @interval,
                liczba_poprawnych_powtorek = @repetitions
            WHERE id_profilu = @profileId 
                AND id_tlumaczenia = @id";

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@nextReview", nextReviewDate);
                cmd.Parameters.AddWithValue("@ef", ef);
                cmd.Parameters.AddWithValue("@interval", interval);
                cmd.Parameters.AddWithValue("@repetitions", n);
                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
                cmd.Parameters.AddWithValue("@id", card.TranslationId);

                cmd.ExecuteNonQuery();
            }
        }

        public List<string> GetVocabularyWordsByTopic(int topicId)
        {
            var words = new List<string>();
            using (var connection = GetConnection())
            {
                connection.Open();
                // Pobieramy angielskie tłumaczenie z tabeli tlumaczenie (id_jezyka = 1)
                string query = @"
            SELECT t.nazwa 
            FROM tlumaczenie t
            JOIN slowko s ON t.id_slowka = s.id
            WHERE s.id_tematu = @topicId AND t.id_jezyka = 1";

                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@topicId", topicId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        words.Add(reader.GetString("nazwa"));
                    }
                }
            }
            return words;
        }

        // Pobiera nazwe/opis zagadnienia gramatycznego na podstawie jego ID
        public string GetGrammarTopicName(int grammarId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT nazwa FROM gramatyka WHERE id = @grammarId", connection);
                command.Parameters.AddWithValue("@grammarId", grammarId);

                var result = command.ExecuteScalar();
                return result?.ToString() ?? string.Empty;
            }
        }
        public List<SelectedTopicItem> GetSelectedTopics()
        {
            var selectedTopics = new List<SelectedTopicItem>();

            using (var connection = GetConnection())
            {
                connection.Open();

                string query = @"
            SELECT 
                tem.id AS TopicId,
                dz.nazwa AS CategoryName,
                tem.nazwa AS TopicName
            FROM profil_temat pt
            JOIN temat tem ON pt.id_tematu = tem.id
            JOIN dzial dz ON tem.id_dzialu = dz.id
            WHERE pt.id_profilu = @profileId 
                AND pt.czy_wybrany = 1
            ORDER BY dz.nazwa ASC, tem.nazwa ASC";

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        selectedTopics.Add(new SelectedTopicItem
                        {
                            TopicId = reader.GetInt32("TopicId"),
                            CategoryName = reader.GetString("CategoryName"),
                            TopicName = reader.GetString("TopicName")
                        });
                    }
                }
            }

            return selectedTopics;
        }
        public List<GrammarModel> GetGrammarTopics()
        {
            var list = new List<GrammarModel>();

            using (var connection = GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT id, nazwa FROM gramatyka", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new GrammarModel
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("nazwa")
                        });
                    }
                }
            }

            return list;
        }
    }
}