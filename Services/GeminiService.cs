using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetEnv;
using LearnLanguages.Models;

namespace LearnLanguages.Services
{
    public static class GeminiService
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<List<ExerciseTaskModel>> GenerateTaskForSingleWordAsync(string word)
        {
            string prompt = $@"
Jesteś profesjonalnym nauczycielem języka angielskiego. Przygotuj dokładnie 1 zadanie testowe typu 'wypełnij lukę' (fill in the blank) w formacie JSON dla słówka: '{word}'.
Zadanie w tablicy JSON (musi być tablica z 1 obiektem) musi posiadać następujące pola:
- 'TrescZdania': zdanie w języku angielskim z luką oznaczoną jako '_____', w które wpisuje się to słowo.
- 'Opcje': tablica składająca się dokładnie z 4 stringów (opcje A, B, C, D, gdzie jedna z opcji to dokładnie to słowo, a pozostałe 3 to sensowne dystraktory).
- 'PoprawnaOdpowiedz': string zawierający dokładnie tę opcję z tablicy 'Opcje', która jest poprawna (czyli to słowo).
- 'Wyjasnienie': krótkie wyjaśnienie w języku polskim.

Zwróć TYLKO I WYŁĄCZNIE czysty kod JSON (bez znaczników markdown typu ```json ... ```), będący tablicą z jednym obiektem: [ {{}} ]";

            int maxRetries = 3;
            int delayMs = 2000;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await CallGeminiApiAsync(prompt);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("503") && i < maxRetries - 1)
                    {
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                        continue;
                    }
                    throw;
                }
            }
            return new List<ExerciseTaskModel>();
        }

        public static async Task<List<ExerciseTaskModel>> GenerateGrammarTasksAsync(int grammarId, int count = 1)
        {
            string grammarTopic = DatabaseService.GetGrammarTopicName(grammarId);
            if (string.IsNullOrEmpty(grammarTopic)) return new List<ExerciseTaskModel>();

            string prompt = $@"
Jesteś profesjonalnym nauczycielem języka angielskiego. Przygotuj dokładnie 1 zadanie testowe typu 'wypełnij lukę' (fill in the blank) z gramatyki w formacie JSON.
Zagadnienie gramatyczne to: '{grammarTopic}'.
Pola w tablicy JSON (z 1 obiektem):
- 'TrescZdania': zdanie z luką '_____'.
- 'Opcje': 4 stringi (A, B, C, D).
- 'PoprawnaOdpowiedz': poprawna opcja.
- 'Wyjasnienie': wyjaśnienie po polsku.

Zwróć TYLKO czysty JSON jako tablicę z 1 elementem: [ {{}} ]";

            return await CallGeminiApiAsync(prompt);
        }

        private static async Task<List<ExerciseTaskModel>> CallGeminiApiAsync(string prompt)
        {
            Env.Load();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Brak klucza GEMINI_API_KEY w pliku .env lub zmienna nie jest widoczna!");
            }

            // Konfiguracja handlera, aby ominąć potencjalne blokady certyfikatów SSL przez antywirusy
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(100);

                string url = $"https://generativelanguage.googleapis.com/v1/models/gemini-3.8-flash:generateContent?key={apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
                };

                string jsonPayload = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(url, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"API odpowiedziało błędem ({response.StatusCode}): {responseString}");
                    }

                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;
                    string rawText = root.GetProperty("candidates")[0]
                                         .GetProperty("content")
                                         .GetProperty("parts")[0]
                                         .GetProperty("text")
                                         .GetString();

                    rawText = rawText.Trim();
                    if (rawText.StartsWith("```json")) rawText = rawText.Substring(7);
                    else if (rawText.StartsWith("```")) rawText = rawText.Substring(3);
                    if (rawText.EndsWith("```")) rawText = rawText.Substring(0, rawText.Length - 3);
                    rawText = rawText.Trim();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<ExerciseTaskModel>>(rawText, options) ?? new List<ExerciseTaskModel>();
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("Timeout: Żądanie zostało przerwane po 20 sekundach. Sprawdź, czy program antywirusowy (np. Avast/Kaspersky) nie skanuje i nie blokuje ruchu aplikacji.");
                }
            }
        }
    }
}