
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

        private const string Model = "openai/gpt-oss-20b";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        static GroqService()
        {
            // Domyślna walidacja certyfikatów SSL.
            // Nie wyłączamy zabezpieczeń HTTPS.
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.groq.com/"),
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public static async Task<List<ExerciseTaskModel>>
            GenerateBatchTasksForWordsAsync(
                List<string> words,
                int count = 5)
        {
            if (words == null || words.Count == 0)
                return new List<ExerciseTaskModel>();

            if (count <= 0)
                throw new ArgumentException(
                    "Liczba zadań musi być większa od zera.");

            var random = new Random();

            var selectedWords = words
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(_ => random.Next())
                .Take(count)
                .ToList();

            if (selectedWords.Count == 0)
                return new List<ExerciseTaskModel>();

            string wordsJoined = string.Join(", ", selectedWords);

            string prompt = $@"
Jesteś profesjonalnym nauczycielem języka angielskiego.

Przygotuj dokładnie {selectedWords.Count} zadań testowych typu fill in the blank.

Słówka:
{wordsJoined}

ZASADY:

1. TrescZdania musi być w całości po angielsku.
2. Luka musi być oznaczona jako _____.
3. Opcje muszą zawierać dokładnie 4 różne odpowiedzi w języku angielskim.
4. Dokładnie jedna odpowiedź musi być poprawna.
5. PoprawnaOdpowiedz musi zawierać dokładny tekst poprawnej opcji z tablicy Opcje.
6. Wyjasnienie musi być krótkie i napisane po polsku.
7. Każde zadanie musi dotyczyć jednego ze wskazanych słówek.
8. Nie powtarzaj zdań ani odpowiedzi.
9. Dystraktory muszą być sensowne i gramatycznie możliwe, ale nie mogą być poprawne w danym kontekście.

Zwróć obiekt zawierający pole tasks z dokładnie {selectedWords.Count} zadaniami.";

            return await CallGroqApiAsync(
                prompt, selectedWords.Count);
        }

        public static async Task<List<ExerciseTaskModel>>
            GenerateGrammarTasksAsync(
                int grammarId,
                int count = 5)
        {
            if (count <= 0)
                throw new ArgumentException(
                    "Liczba zadań musi być większa od zera.");

            string grammarTopic =
                DatabaseService.GetGrammarTopicName(grammarId);

            if (string.IsNullOrWhiteSpace(grammarTopic))
                return new List<ExerciseTaskModel>();

            string prompt = $@"
Jesteś profesjonalnym nauczycielem języka angielskiego.

Przygotuj dokładnie {count} różnorodnych zadań testowych
typu fill in the blank.

ZAGADNIENIE GRAMATYCZNE:
{grammarTopic}

ZASADY:

1. Zdania muszą być napisane po angielsku.
2. Luka musi być oznaczona jako _____.
3. Luka musi wymagać użycia pełnej formy gramatycznej,
   a nie tylko pojedynczego słowa.
4. Możesz podać bezokolicznik czasownika w nawiasie.
5. Opcje muszą zawierać dokładnie 4 różne odpowiedzi.
6. Opcje muszą być w języku angielskim.
7. Dokładnie jedna odpowiedź musi być poprawna.
8. PoprawnaOdpowiedz musi zawierać dokładny tekst
   poprawnej opcji z tablicy Opcje.
9. Wyjasnienie musi być napisane po polsku
   i wyjaśniać zastosowaną konstrukcję gramatyczną.
10. Dystraktory powinny przedstawiać typowe błędy gramatyczne.
11. Nie powtarzaj zdań ani pytań.

Zwróć dokładnie {count} różnych zadań.";

            return await CallGroqApiAsync(prompt, count);
        }

        private static async Task<List<ExerciseTaskModel>>
            CallGroqApiAsync(string prompt, int expectedCount)
        {
            Env.Load();

            string apiKey = Environment
                .GetEnvironmentVariable("GROQ_API_KEY")
                ?.Trim()
                .Trim('"', '\'');

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "Brak klucza GROQ_API_KEY w pliku .env.");
            }

            // Ścisły schemat odpowiedzi JSON.
            var responseSchema = new
            {
                type = "object",

                properties = new
                {
                    tasks = new
                    {
                        type = "array",

                        items = new
                        {
                            type = "object",

                            properties = new
                            {
                                TrescZdania = new
                                {
                                    type = "string"
                                },

                                Opcje = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "string"
                                    }
                                },

                                PoprawnaOdpowiedz = new
                                {
                                    type = "string"
                                },

                                Wyjasnienie = new
                                {
                                    type = "string"
                                }
                            },

                            required = new[]
                            {
                                "TrescZdania",
                                "Opcje",
                                "PoprawnaOdpowiedz",
                                "Wyjasnienie"
                            },

                            additionalProperties = false
                        }
                    }
                },

                required = new[]
                {
                    "tasks"
                },

                additionalProperties = false
            };

            var requestBody = new
            {
                model = Model,

                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content =
                            "Jesteś profesjonalnym nauczycielem języka " +
                            "angielskiego. Generujesz wysokiej jakości " +
                            "zadania edukacyjne. Zawsze zwracasz dane " +
                            "zgodne z wymaganym schematem."
                    },

                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },

                temperature = 0.3,

                max_completion_tokens = 4096,

                reasoning_effort = "low",

                reasoning_format = "hidden",

                response_format = new
                {
                    type = "json_schema",

                    json_schema = new
                    {
                        name = "english_exercises",

                        strict = true,

                        schema = responseSchema
                    }
                }
            };

            string jsonPayload =
                JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "openai/v1/chat/completions");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            request.Content = new StringContent(
                jsonPayload,
                Encoding.UTF8,
                "application/json");

            using var response =
                await httpClient.SendAsync(request);

            string responseString =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Groq API ERROR: {(int)response.StatusCode} " +
                    $"{response.StatusCode}\n" +
                    $"Odpowiedź serwera: {responseString}");
            }

            try
            {
                using var document =
                    JsonDocument.Parse(responseString);

                var root = document.RootElement;

                if (!root.TryGetProperty("choices", out var choices) ||
                    choices.GetArrayLength() == 0)
                {
                    throw new Exception(
                        "Groq nie zwrócił żadnej odpowiedzi.");
                }

                var message = choices[0]
                    .GetProperty("message");

                if (message.TryGetProperty("refusal", out var refusal))
                {
                    throw new Exception(
                        $"Model odmówił wygenerowania zadań: " +
                        refusal.GetString());
                }

                if (!message.TryGetProperty("content", out var contentElement) ||
                    contentElement.ValueKind != JsonValueKind.String)
                {
                    throw new Exception(
                        "Model nie zwrócił treści JSON.");
                }

                string rawText = contentElement.GetString();

                if (string.IsNullOrWhiteSpace(rawText))
                    throw new Exception("Odpowiedź modelu jest pusta.");

                using var jsonDocument =
                    JsonDocument.Parse(rawText);

                var jsonRoot = jsonDocument.RootElement;

                if (!jsonRoot.TryGetProperty(
                    "tasks", out var tasksElement) ||
                    tasksElement.ValueKind != JsonValueKind.Array)
                {
                    throw new Exception(
                        "Brak poprawnej tablicy tasks.");
                }

                if (tasksElement.GetArrayLength() != expectedCount)
                {
                    throw new Exception(
                        $"Nieprawidłowa liczba zadań. " +
                        $"Oczekiwano: {expectedCount}, " +
                        $"otrzymano: {tasksElement.GetArrayLength()}.");
                }

                // Dodatkowa kontrola danych przed deserializacją.
                foreach (var task in tasksElement.EnumerateArray())
                {
                    string[] requiredFields =
                    {
                        "TrescZdania",
                        "Opcje",
                        "PoprawnaOdpowiedz",
                        "Wyjasnienie"
                    };

                    foreach (string field in requiredFields)
                    {
                        if (!task.TryGetProperty(field, out var value) ||
                            value.ValueKind != JsonValueKind.String &&
                            value.ValueKind != JsonValueKind.Array)
                        {
                            throw new Exception(
                                $"Nieprawidłowe lub brakujące pole: {field}");
                        }
                    }

                    var options = task.GetProperty("Opcje");

                    if (options.ValueKind != JsonValueKind.Array ||
                        options.GetArrayLength() != 4)
                    {
                        throw new Exception(
                            "Każde zadanie musi mieć dokładnie 4 opcje.");
                    }

                    var optionValues = options
                        .EnumerateArray()
                        .Select(x => x.GetString())
                        .ToList();

                    if (optionValues.Any(
                        x => string.IsNullOrWhiteSpace(x)))
                    {
                        throw new Exception(
                            "Opcje nie mogą być puste.");
                    }

                    if (optionValues.Distinct(
                        StringComparer.OrdinalIgnoreCase).Count() != 4)
                    {
                        throw new Exception(
                            "Opcje odpowiedzi muszą być różne.");
                    }

                    string correctAnswer = task
                        .GetProperty("PoprawnaOdpowiedz")
                        .GetString();

                    if (!optionValues.Contains(
                        correctAnswer,
                        StringComparer.Ordinal))
                    {
                        throw new Exception(
                            "Poprawna odpowiedź nie występuje w tablicy Opcje.");
                    }

                    if (string.IsNullOrWhiteSpace(
                        task.GetProperty("TrescZdania").GetString()) ||
                        string.IsNullOrWhiteSpace(
                        task.GetProperty("Wyjasnienie").GetString()))
                    {
                        throw new Exception(
                            "Treść zadania lub wyjaśnienie jest puste.");
                    }
                }

                string tasksJson = tasksElement.GetRawText();

                var result =
                    JsonSerializer.Deserialize<List<ExerciseTaskModel>>(
                        tasksJson,
                        JsonOptions);

                if (result == null || result.Count != expectedCount)
                {
                    throw new Exception(
                        "Nie udało się poprawnie odczytać zadań.");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new Exception(
                    $"Błąd parsowania JSON z Groq: {ex.Message}\n" +
                    $"Odpowiedź: {responseString}", ex);
            }
        }
    }
}
