using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetEnv;
using LearnLanguages.Models;

namespace LearnLanguages.Services
{
    public static class GroqService
    {
        private static readonly HttpClient httpClient;

        static GroqService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            // USTAWIANIE BaseAddress eliminuje problem z "invalid request URI"
            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.groq.com/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public static async Task<List<ExerciseTaskModel>> GenerateBatchTasksForWordsAsync(List<string> words, int count = 5)
        {
            var random = new Random();
            var selectedWords = words.OrderBy(x => random.Next()).Take(count).ToList();
            string wordsJoined = string.Join(", ", selectedWords);

            string prompt = $@"
Jesteś profesjonalnym nauczycielem języka angielskiego. Przygotuj dokładnie {selectedWords.Count} zadań testowych typu 'wypełnij lukę' (fill in the blank) w formacie JSON dla następujących słówek: [{wordsJoined}].
Zwróć tablicę JSON zawierającą dokładnie {selectedWords.Count} obiektów. Każdy obiekt musi posiadać pola:
- 'TrescZdania': zdanie w języku angielskim z luką oznaczoną jako '_____', w które wpisuje się dane słowo.
- 'Opcje': tablica składająca się dokładnie z 4 stringów (opcje A, B, C, D, gdzie jedna z opcji to dokładnie to słowo, a pozostałe 3 to sensowne dystraktory).
- 'PoprawnaOdpowiedz': string zawierający dokładnie tę opcję z tablicy 'Opcje', która jest poprawna.
- 'Wyjasnienie': krótkie wyjaśnienie w języku polskim.

Zwróć TYLKO I WYŁĄCZNIE czysty kod JSON będący tablicą obiektów [ {{}}, {{}} ] (bez żadnych znaczników markdown typu ```json).";

            return await CallGroqApiAsync(prompt);
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

            return await CallGroqApiAsync(prompt);
        }

        private static async Task<List<ExerciseTaskModel>> CallGroqApiAsync(string prompt)
        {
            Env.Load();
            string apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")?.Trim().Trim('"', '\'');

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Brak klucza GROQ_API_KEY w pliku .env lub zmienna nie jest widoczna!");
            }

            // Podajemy tylko ścieżkę względną, ponieważ BaseAddress jest już skonfigurowany w konstruktorze statycznym
            string endpoint = "openai/v1/chat/completions";

            var requestBody = new
            {
                model = "openai/gpt-oss-20b", // Wybrany model z darmowego planu
                messages = new[]
    {
        new
        {
            role = "user",
            content = prompt
        }
    },
                temperature = 0.7,
                response_format = new { type = "json_object" }
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Groq API odpowiedziało błędem ({response.StatusCode}): {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            string rawText = root.GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();

            rawText = rawText.Trim();
            if (rawText.StartsWith("```json")) rawText = rawText.Substring(7);
            else if (rawText.StartsWith("```")) rawText = rawText.Substring(3);

            if (rawText.EndsWith("```"))
            {
                rawText = rawText.Substring(0, rawText.Length - 3).Trim();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (rawText.StartsWith("{"))
            {
                var singleItem = JsonSerializer.Deserialize<ExerciseTaskModel>(rawText, options);
                return singleItem != null ? new List<ExerciseTaskModel> { singleItem } : new List<ExerciseTaskModel>();
            }

            return JsonSerializer.Deserialize<List<ExerciseTaskModel>>(rawText, options) ?? new List<ExerciseTaskModel>();
        }
    }
}