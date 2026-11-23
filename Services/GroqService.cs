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
Jesteś profesjonalnym nauczycielem języka angielskiego. Przygotuj dokładnie {selectedWords.Count} zadań testowych typu 'wypełnij lukę' (fill in the blank) w formacie JSON dla następujących angielskich słówek: [{wordsJoined}].
Zasady:
1. Zdanie ('TrescZdania') musi być w całości w języku angielskim z luką oznaczoną jako '_____'.
2. Wszystkie opcje w tablicy 'Opcje' (4 stringi: A, B, C, D) muszą być w języku angielskim. Jedna opcja to poszukiwane słowo, pozostałe 3 to sensowne dystraktory.
3. 'PoprawnaOdpowiedz' MUSI zawierać DOKŁADNY TEKST poprawnej opcji z tablicy 'Opcje' (np. jeśli poprawna opcja to 'read', wpisz 'read', a NIE literę 'A' ani 'Opcja A').
4. 'Wyjasnienie' ma być krótkie i w języku polskim.

Zwróć obiekt JSON posiadający pole 'tasks' będące tablicą zawierającą dokładnie {selectedWords.Count} obiektów posiadających pola: 'TrescZdania', 'Opcje', 'PoprawnaOdpowiedz', 'Wyjasnienie'.
Zwróć TYLKO I WYŁĄCZNIE czysty kod JSON w formacie: {{ ""tasks"": [ {{}}, {{}} ] }} (bez żadnych znaczników markdown typu ```json).";

            return await CallGroqApiAsync(prompt);
        }

        public static async Task<List<ExerciseTaskModel>> GenerateGrammarTasksAsync(int grammarId, int count = 5)
        {
            string grammarTopic = DatabaseService.GetGrammarTopicName(grammarId);
            if (string.IsNullOrEmpty(grammarTopic)) return new List<ExerciseTaskModel>();

            string prompt = $@"
Jesteś profesjonalnym nauczycielem języka angielskiego. Przygotuj dokładnie {count} różnorodnych zadań testowych typu 'wypełnij lukę' (fill in the blank) z gramatyki w formacie JSON.
Zagadnienie gramatyczne to: '{grammarTopic}'.

Kluczowe zasady dotyczące konstrukcji zdań i odpowiedzi:
1. Zdania ('TrescZdania') muszą zawierać lukę '_____' w miejscu, które wymaga użycia **pełnej formy czasownika w danym czasie**, co oznacza, że luka **musi wymagać wpisania zarówno operatora/posiłkowego (np. have, had, will, did, do, are), jak i czasownika właściwego** (np. 'had finished', 'have been waiting', 'will go', 'did not see', 'is playing'), a nie tylko pojedynczego słowa.
2. W nawiasie obok luki możesz podać bezokolicznik czasownika w celach pomocniczych, np. 'By the time we arrived, he _____ (finish) his work.' lub konstruować zdania tak, aby kontekst jednoznacznie wskazywał formę.
3. Wszystkie opcje w tablicy 'Opcje' (4 stringi: A, B, C, D) muszą być w języku angielskim i mogą składać się z dwóch lub więcej słów (np. 'had finished', 'has finished', 'finished', 'was finishing'). Dokładnie jeden jest poprawny, a pozostałe to typowe dystraktory gramatyczne (błędy w operatorze lub formie).
4. 'PoprawnaOdpowiedz' MUSI zawierać DOKŁADNY TEKST poprawnej opcji z tablicy 'Opcje' (np. 'had finished').
5. 'Wyjasnienie' ma być w języku polskim i tłumaczyć, dlaczego użyto takiej formy gramatycznej (w tym operatora i czasownika).

Zwróć obiekt JSON z polem 'tasks' zawierającym tablicę z dokładnie {count} obiektami posiadającymi pola: 'TrescZdania', 'Opcje', 'PoprawnaOdpowiedz', 'Wyjasnienie'.
Zwróć TYLKO czysty JSON w formacie: {{ ""tasks"": [ {{}}, {{}} ] }} (bez znaczników markdown).";

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

            string endpoint = "openai/v1/chat/completions";

            var requestBody = new
            {
                model = "openai/gpt-oss-20b",
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

            try
            {
                using var jsonDoc = JsonDocument.Parse(rawText);
                var rootElement = jsonDoc.RootElement;

                if (rootElement.ValueKind == JsonValueKind.Object && rootElement.TryGetProperty("tasks", out var tasksElement))
                {
                    string tasksJson = tasksElement.GetRawText();
                    return JsonSerializer.Deserialize<List<ExerciseTaskModel>>(tasksJson, options) ?? new List<ExerciseTaskModel>();
                }
                else if (rootElement.ValueKind == JsonValueKind.Object)
                {
                    var singleItem = JsonSerializer.Deserialize<ExerciseTaskModel>(rawText, options);
                    return singleItem != null ? new List<ExerciseTaskModel> { singleItem } : new List<ExerciseTaskModel>();
                }
                else if (rootElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<ExerciseTaskModel>>(rawText, options) ?? new List<ExerciseTaskModel>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Błąd parsowania JSON zwróconego przez Groq: {ex.Message}. Surowy tekst: {rawText}");
            }

            return new List<ExerciseTaskModel>();
        }
    }
}