using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LearnLanguages.Models;
using Microsoft.Data.Sqlite;

namespace LearnLanguages.Services
{
    public class SqliteDatabaseService : IDatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public ProfileModel CurrentProfile { get; set; }

        private int CurrentProfileId =>
            CurrentProfile?.Id ?? throw new InvalidOperationException("Nie wybrano profilu użytkownika.");

        public SqliteDatabaseService()
        {
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LearnLanguages");

            Directory.CreateDirectory(appDataFolder);

            _dbPath = Path.Combine(appDataFolder, "learnlanguages.db");
            _connectionString = $"Data Source={_dbPath}";

            EnsureDatabaseInitialized();
        }

        private SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Włączenie obsługi kluczy obcych (wymagane m.in. dla ON DELETE CASCADE przy usuwaniu profilu)
            using var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            pragmaCmd.ExecuteNonQuery();

            return connection;
        }

        public bool TestConnection(out string errorMessage)
        {
            try
            {
                using var connection = GetConnection();
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private void EnsureDatabaseInitialized()
        {
            if (File.Exists(_dbPath))
            {
                try
                {
                    using var checkConn = GetConnection();
                    using var checkCmd = new SqliteCommand(
                        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Jezyk';",
                        checkConn);
                    long tableExists = Convert.ToInt64(checkCmd.ExecuteScalar());

                    if (tableExists > 0)
                    {
                        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM Jezyk;", checkConn);
                        if (Convert.ToInt64(countCmd.ExecuteScalar()) > 0)
                        {
                            return; // Baza już istnieje i zawiera dane — zachowujemy postępy użytkownika
                        }
                    }
                }
                catch
                {
                    // Jeśli plik był uszkodzony lub pusty, odtworzymy go poniżej
                }

                SqliteConnection.ClearAllPools();
                File.Delete(_dbPath);
            }

            string initSqlPath = FindInitSqlPath();
            string rawSql = File.ReadAllText(initSqlPath, Encoding.UTF8);

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Tworzenie struktury tabel w SQLite
                string createTablesSql = @"
                    CREATE TABLE IF NOT EXISTS Jezyk (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        nazwa TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Dzial (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        nazwa TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Temat (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        id_dzialu INTEGER NOT NULL,
                        nazwa TEXT NOT NULL,
                        FOREIGN KEY (id_dzialu) REFERENCES Dzial(id) ON DELETE CASCADE ON UPDATE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS Slowko (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        id_tematu INTEGER NOT NULL,
                        nazwa TEXT NOT NULL,
                        grafika TEXT,
                        FOREIGN KEY (id_tematu) REFERENCES Temat(id) ON DELETE CASCADE ON UPDATE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS Tlumaczenie (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        id_slowka INTEGER NOT NULL,
                        id_jezyka INTEGER NOT NULL,
                        nazwa TEXT NOT NULL,
                        grafika TEXT,
                        FOREIGN KEY (id_slowka) REFERENCES Slowko(id) ON DELETE CASCADE ON UPDATE CASCADE,
                        FOREIGN KEY (id_jezyka) REFERENCES Jezyk(id) ON DELETE CASCADE ON UPDATE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS Gramatyka (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        id_jezyka INTEGER NOT NULL,
                        nazwa TEXT NOT NULL,
                        FOREIGN KEY (id_jezyka) REFERENCES Jezyk(id) ON DELETE CASCADE ON UPDATE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS profil (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        nazwa TEXT NOT NULL UNIQUE,
                        data_utworzenia TEXT DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS profil_temat (
                        id_profilu INTEGER NOT NULL,
                        id_tematu INTEGER NOT NULL,
                        czy_wybrany INTEGER NOT NULL DEFAULT 1,
                        PRIMARY KEY (id_profilu, id_tematu),
                        FOREIGN KEY (id_profilu) REFERENCES profil(id) ON DELETE CASCADE,
                        FOREIGN KEY (id_tematu) REFERENCES Temat(id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS profil_postep (
                        id_profilu INTEGER NOT NULL,
                        id_tlumaczenia INTEGER NOT NULL,
                        wspolczynnik_latwosci REAL NOT NULL DEFAULT 2.5,
                        odstep_powtorek INTEGER NOT NULL DEFAULT 0,
                        liczba_poprawnych_powtorek INTEGER NOT NULL DEFAULT 0,
                        data_nastepnej_powtorki TEXT NOT NULL,
                        PRIMARY KEY (id_profilu, id_tlumaczenia),
                        FOREIGN KEY (id_profilu) REFERENCES profil(id) ON DELETE CASCADE,
                        FOREIGN KEY (id_tlumaczenia) REFERENCES Tlumaczenie(id) ON DELETE CASCADE
                    );";

                using (var schemaCmd = new SqliteCommand(createTablesSql, connection, transaction))
                {
                    schemaCmd.ExecuteNonQuery();
                }

                // 2. Filtrowanie i wykonanie samych instrukcji INSERT z pliku init.sql
                var filteredLines = new List<string>();
                foreach (string line in rawSql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("SET ", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("TRUNCATE ", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("UPDATE ", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("WHERE grafika", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    filteredLines.Add(line);
                }

                string insertScript = string.Join("\n", filteredLines);
                using (var insertCmd = new SqliteCommand(insertScript, connection, transaction))
                {
                    insertCmd.ExecuteNonQuery();
                }

                // 3. Zamiana rozszerzeń .pdf na .png w składni SQLite
                string updateImagesSql = @"
                    UPDATE Slowko
                    SET grafika = SUBSTR(grafika, 1, LENGTH(grafika) - 4) || '.png'
                    WHERE grafika LIKE '%.pdf';

                    UPDATE Tlumaczenie
                    SET grafika = SUBSTR(grafika, 1, LENGTH(grafika) - 4) || '.png'
                    WHERE grafika LIKE '%.pdf';";

                using (var updateCmd = new SqliteCommand(updateImagesSql, connection, transaction))
                {
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static string FindInitSqlPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string outputPath = Path.Combine(baseDir, "database", "init.sql");
            if (File.Exists(outputPath)) return outputPath;

            string devPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\database\init.sql"));
            if (File.Exists(devPath)) return devPath;

            throw new FileNotFoundException("Nie znaleziono pliku inicjalizacyjnego bazy danych: database/init.sql");
        }

        public List<ProfileModel> GetProfiles()
        {
            var profiles = new List<ProfileModel>();

            using var connection = GetConnection();
            var cmd = new SqliteCommand("SELECT id, nazwa FROM profil ORDER BY id ASC", connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                profiles.Add(new ProfileModel
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["nazwa"].ToString()
                });
            }

            return profiles;
        }

        public void CreateProfile(string name)
        {
            using var connection = GetConnection();
            var cmd = new SqliteCommand("INSERT INTO profil (nazwa) VALUES (@name)", connection);
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.ExecuteNonQuery();
        }

        public void DeleteProfile(int profileId)
        {
            using var connection = GetConnection();
            var cmd = new SqliteCommand("DELETE FROM profil WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("@id", profileId);
            cmd.ExecuteNonQuery();
        }

        public List<Department> GetDepartments()
        {
            var departments = new List<Department>();

            using var connection = GetConnection();
            var cmd = new SqliteCommand("SELECT id, nazwa FROM dzial", connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                departments.Add(new Department
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["nazwa"].ToString()
                });
            }

            return departments;
        }

        public List<Topic> GetTopicsByDepartment(int departmentId)
        {
            var topics = new List<Topic>();

            using var connection = GetConnection();
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

            var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            cmd.Parameters.AddWithValue("@departmentId", departmentId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                topics.Add(new Topic
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    DepartmentId = Convert.ToInt32(reader["DepartmentId"]),
                    Name = reader["Name"].ToString(),
                    IsSelected = Convert.ToBoolean(reader["IsSelected"]),
                    WordsCount = Convert.ToInt32(reader["WordsCount"])
                });
            }

            return topics;
        }

        public void MarkTopicAsSelected(int topicId)
        {
            using var connection = GetConnection();
            int profileId = CurrentProfileId;

            var cmd1 = new SqliteCommand(@"
                INSERT INTO profil_temat (id_profilu, id_tematu, czy_wybrany)
                VALUES (@profileId, @topicId, 1)
                ON CONFLICT(id_profilu, id_tematu) DO UPDATE SET czy_wybrany = 1", connection);

            cmd1.Parameters.AddWithValue("@profileId", profileId);
            cmd1.Parameters.AddWithValue("@topicId", topicId);
            cmd1.ExecuteNonQuery();

            var cmd2 = new SqliteCommand(@"
                INSERT OR IGNORE INTO profil_postep 
                    (id_profilu, id_tlumaczenia, wspolczynnik_latwosci, odstep_powtorek, liczba_poprawnych_powtorek, data_nastepnej_powtorki)
                SELECT @profileId, t.id, 2.5, 0, 0, @dzisiaj
                FROM tlumaczenie t
                JOIN slowko s ON t.id_slowka = s.id
                WHERE s.id_tematu = @topicId AND t.id_jezyka = 1", connection);

            cmd2.Parameters.AddWithValue("@profileId", profileId);
            cmd2.Parameters.AddWithValue("@dzisiaj", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd2.Parameters.AddWithValue("@topicId", topicId);
            cmd2.ExecuteNonQuery();
        }

        public void UnmarkTopicAsSelected(int topicId)
        {
            using var connection = GetConnection();
            var cmd = new SqliteCommand(@"
                UPDATE profil_temat 
                SET czy_wybrany = 0 
                WHERE id_profilu = @profileId AND id_tematu = @topicId", connection);

            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            cmd.Parameters.AddWithValue("@topicId", topicId);
            cmd.ExecuteNonQuery();
        }

        public Dictionary<DateTime, int> GetReviewCountsForMonth(int year, int month)
        {
            var result = new Dictionary<DateTime, int>();

            using var connection = GetConnection();
            var cmd = new SqliteCommand(@"
                SELECT DATE(pp.data_nastepnej_powtorki) AS review_date, COUNT(*) AS count 
                FROM profil_postep pp
                JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
                JOIN slowko s ON t.id_slowka = s.id
                JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
                WHERE pp.id_profilu = @profileId
                  AND pt.czy_wybrany = 1
                  AND strftime('%Y', pp.data_nastepnej_powtorki) = @year 
                  AND strftime('%m', pp.data_nastepnej_powtorki) = @month
                  AND DATE(pp.data_nastepnej_powtorki) >= DATE('now', 'localtime')
                  AND t.id_jezyka = 1
                GROUP BY DATE(pp.data_nastepnej_powtorki)", connection);

            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            cmd.Parameters.AddWithValue("@year", year.ToString("D4"));
            cmd.Parameters.AddWithValue("@month", month.ToString("D2"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader["review_date"] != DBNull.Value)
                {
                    DateTime date = DateTime.Parse(reader["review_date"].ToString());
                    int count = Convert.ToInt32(reader["count"]);
                    result[date] = count;
                }
            }

            return result;
        }

        public int GetOverdueReviewCount()
        {
            using var connection = GetConnection();
            var cmd = new SqliteCommand(@"
                SELECT COUNT(*) 
                FROM profil_postep pp
                JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
                JOIN slowko s ON t.id_slowka = s.id
                JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
                WHERE pp.id_profilu = @profileId
                  AND pt.czy_wybrany = 1
                  AND DATE(pp.data_nastepnej_powtorki) < DATE('now', 'localtime')
                  AND t.id_jezyka = 1", connection);

            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int GetReviewCountForToday()
        {
            using var connection = GetConnection();
            var cmd = new SqliteCommand(@"
                SELECT COUNT(*) 
                FROM profil_postep pp
                JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
                JOIN slowko s ON t.id_slowka = s.id
                JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
                WHERE pp.id_profilu = @profileId
                  AND pt.czy_wybrany = 1
                  AND DATE(pp.data_nastepnej_powtorki) <= DATE('now', 'localtime')
                  AND t.id_jezyka = 1", connection);

            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<TopicReviewSummary> GetTopicReviewSummariesForToday()
        {
            var summaries = new List<TopicReviewSummary>();

            using var connection = GetConnection();
            string query = @"
                SELECT 
                    tem.id AS TopicId,
                    tem.nazwa AS TopicName,
                    dz.nazwa AS CategoryName,
                    SUM(CASE WHEN DATE(pp.data_nastepnej_powtorki) = DATE('now', 'localtime') THEN 1 ELSE 0 END) AS TodayCount,
                    SUM(CASE WHEN DATE(pp.data_nastepnej_powtorki) < DATE('now', 'localtime') THEN 1 ELSE 0 END) AS OverdueCount
                FROM profil_postep pp
                JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
                JOIN slowko s ON t.id_slowka = s.id
                JOIN temat tem ON s.id_tematu = tem.id
                JOIN dzial dz ON tem.id_dzialu = dz.id
                JOIN profil_temat pt ON pt.id_tematu = tem.id AND pt.id_profilu = pp.id_profilu
                WHERE pp.id_profilu = @profileId
                  AND pt.czy_wybrany = 1
                  AND DATE(pp.data_nastepnej_powtorki) <= DATE('now', 'localtime')
                  AND t.id_jezyka = 1
                GROUP BY tem.id, tem.nazwa, dz.nazwa
                ORDER BY OverdueCount DESC, dz.nazwa ASC, tem.nazwa ASC";

            var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                summaries.Add(new TopicReviewSummary
                {
                    TopicId = Convert.ToInt32(reader["TopicId"]),
                    TopicName = reader["TopicName"].ToString(),
                    CategoryName = reader["CategoryName"].ToString(),
                    TodayCount = Convert.ToInt32(reader["TodayCount"]),
                    OverdueCount = Convert.ToInt32(reader["OverdueCount"])
                });
            }

            return summaries;
        }

        public int GetUpcomingReviewsCount(int days = 3)
        {
            using var connection = GetConnection();
            string maxDate = DateTime.Today.AddDays(days - 1).ToString("yyyy-MM-dd");

            var cmd = new SqliteCommand(@"
                SELECT COUNT(*) 
                FROM profil_postep pp
                JOIN tlumaczenie t ON pp.id_tlumaczenia = t.id
                JOIN slowko s ON t.id_slowka = s.id
                JOIN profil_temat pt ON pt.id_tematu = s.id_tematu AND pt.id_profilu = pp.id_profilu
                WHERE pp.id_profilu = @profileId
                  AND pt.czy_wybrany = 1
                  AND DATE(pp.data_nastepnej_powtorki) <= @maxDate
                  AND t.id_jezyka = 1", connection);

            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            cmd.Parameters.AddWithValue("@maxDate", maxDate);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool HasUnselectedTopics()
        {
            using var connection = GetConnection();
            var cmd = new SqliteCommand(@"
                SELECT COUNT(*)
                FROM temat tem
                LEFT JOIN profil_temat pt ON pt.id_tematu = tem.id AND pt.id_profilu = @profileId
                WHERE COALESCE(pt.czy_wybrany, 0) = 0", connection);

            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public List<FlashcardModel> GetFlashcardsForToday(int? topicId = null, bool onlyOverdue = false)
        {
            var flashcards = new List<FlashcardModel>();

            using var connection = GetConnection();
            string dateCondition = onlyOverdue
                ? "DATE(pp.data_nastepnej_powtorki) < DATE('now', 'localtime')"
                : "DATE(pp.data_nastepnej_powtorki) <= DATE('now', 'localtime')";

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

            var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            if (topicId.HasValue)
            {
                cmd.Parameters.AddWithValue("@topicId", topicId.Value);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                flashcards.Add(new FlashcardModel
                {
                    TranslationId = Convert.ToInt32(reader["TranslationId"]),
                    TopicId = Convert.ToInt32(reader["TopicId"]),
                    CategoryName = reader["CategoryName"].ToString(),
                    TopicName = reader["TopicName"].ToString(),
                    PolImageName = reader["PolImageName"] == DBNull.Value ? null : reader["PolImageName"].ToString(),
                    EngImageName = reader["EngImageName"] == DBNull.Value ? null : reader["EngImageName"].ToString(),
                    EasinessFactor = reader["EasinessFactor"] == DBNull.Value ? 2.5 : Convert.ToDouble(reader["EasinessFactor"]),
                    Interval = reader["IntervalDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["IntervalDays"]),
                    Repetitions = reader["Repetitions"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Repetitions"]),
                    NextReviewDate = reader["NextReviewDate"] == DBNull.Value
                        ? DateTime.Today
                        : DateTime.Parse(reader["NextReviewDate"].ToString())
                });
            }

            return flashcards;
        }

        public void UpdateFlashcardProgress(FlashcardModel card, bool isCorrect)
        {
            int n = card.Repetitions;
            int interval = card.Interval;
            double ef = card.EasinessFactor;
            DateTime today = DateTime.Today;

            int overdueDays = Math.Max(0, (today - card.NextReviewDate.Date).Days);

            if (isCorrect)
            {
                n++;

                if (n == 1)
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

                ef = Math.Round(ef + 0.10, 2);
            }
            else
            {
                n = 0;
                interval = 1;
                ef = Math.Round(Math.Max(1.3, ef - 0.32), 2);
            }

            DateTime nextReviewDate = today.AddDays(interval);

            using var connection = GetConnection();
            string query = @"
                UPDATE profil_postep 
                SET 
                    data_nastepnej_powtorki = @nextReview,
                    wspolczynnik_latwosci = @ef,
                    odstep_powtorek = @interval,
                    liczba_poprawnych_powtorek = @repetitions
                WHERE id_profilu = @profileId 
                  AND id_tlumaczenia = @id";

            var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@nextReview", nextReviewDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@ef", ef);
            cmd.Parameters.AddWithValue("@interval", interval);
            cmd.Parameters.AddWithValue("@repetitions", n);
            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);
            cmd.Parameters.AddWithValue("@id", card.TranslationId);

            cmd.ExecuteNonQuery();
        }

        public List<string> GetVocabularyWordsByTopic(int topicId)
        {
            var words = new List<string>();

            using var connection = GetConnection();
            string query = @"
                SELECT t.nazwa 
                FROM tlumaczenie t
                JOIN slowko s ON t.id_slowka = s.id
                WHERE s.id_tematu = @topicId AND t.id_jezyka = 1";

            var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@topicId", topicId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                words.Add(reader["nazwa"].ToString());
            }

            return words;
        }

        public string GetGrammarTopicName(int grammarId)
        {
            using var connection = GetConnection();
            var cmd = new SqliteCommand("SELECT nazwa FROM gramatyka WHERE id = @grammarId", connection);
            cmd.Parameters.AddWithValue("@grammarId", grammarId);

            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? string.Empty;
        }

        public List<GrammarModel> GetGrammarTopics()
        {
            var list = new List<GrammarModel>();

            using var connection = GetConnection();
            var cmd = new SqliteCommand("SELECT id, nazwa FROM gramatyka", connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new GrammarModel
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["nazwa"].ToString()
                });
            }

            return list;
        }

        public List<SelectedTopicItem> GetSelectedTopics()
        {
            var selectedTopics = new List<SelectedTopicItem>();

            using var connection = GetConnection();
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

            var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@profileId", CurrentProfileId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                selectedTopics.Add(new SelectedTopicItem
                {
                    TopicId = Convert.ToInt32(reader["TopicId"]),
                    CategoryName = reader["CategoryName"].ToString(),
                    TopicName = reader["TopicName"].ToString()
                });
            }

            return selectedTopics;
        }
    }
}