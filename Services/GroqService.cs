
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNetEnv;
using LearnLanguages.Models;

namespace LearnLanguages.Services
{
    public class GroqService : ILLMService
    {
        private static readonly HttpClient httpClient;
        private readonly IDatabaseService _databaseService;

        private const string Model = "openai/gpt-oss-20b";

        // Maksymalna liczba prób wygenerowania poprawnych zadań.
        private const int MaxGenerationAttempts = 3;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        static GroqService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.groq.com/"),
                Timeout = TimeSpan.FromSeconds(60)
            };
        }
        public GroqService(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        // =====================================================
        // GENEROWANIE ZADAŃ ZE SŁÓWEK
        // =====================================================

        public async Task<List<ExerciseTaskModel>>
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

10. Nie twórz odpowiedzi, które mają takie samo znaczenie, nawet jeśli różnią się zapisem.

Zabronione przykłady:
- don't like / do not like
- doesn't work / does not work
- can't swim / cannot swim
- won't go / will not go
- big / large
- begin / start
- beautiful / pretty

11. Każda odpowiedź musi mieć inne znaczenie.
12. Nie używaj synonimów jako osobnych opcji.
13. Nie używaj skróconych i pełnych form tego samego wyrażenia jako dwóch różnych odpowiedzi.
14. Tylko jedna odpowiedź może być poprawna gramatycznie i znaczeniowo w danym zdaniu.

15. KRYTYCZNA ZASADA DOTYCZĄCA CZASOWNIKÓW:

- Nie powtarzaj tego samego czasownika w jednym zdaniu.
- Nie powtarzaj operatorów ani czasowników pomocniczych w konstrukcji, która tworzy błąd.
- Nie umieszczaj przed luką części konstrukcji gramatycznej, która ma zostać uzupełniona w luce.
- Jeżeli poprawna odpowiedź składa się z kilku słów, wszystkie te słowa muszą znajdować się w luce.
- Nie umieszczaj części poprawnej odpowiedzi przed luką ani za luką.
- Luka musi zastępować całą konstrukcję gramatyczną.

PRZYKŁADY POPRAWNE:

Zdanie: They _____ tomorrow.
Odpowiedź: will arrive.

Zdanie: She _____ her homework.
Odpowiedź: has finished.

Zdanie: He _____ now.
Odpowiedź: is working.

PRZYKŁADY ZABRONIONE:

Zdanie: They will _____ tomorrow.
Odpowiedź: will arrive.

Zdanie: She has _____ her homework.
Odpowiedź: has finished.

Zdanie: He is _____ now.
Odpowiedź: is working.

16. Przed zwróceniem każdego zadania:
- Wstaw poprawną odpowiedź w miejsce _____.
- Sprawdź całe powstałe zdanie.
- Upewnij się, że konstrukcja gramatyczna nie została powtórzona.
- Upewnij się, że zdanie jest naturalne i gramatyczne.
- Jeśli występuje powtórzenie, popraw zadanie.

Zwróć obiekt zawierający pole tasks z dokładnie {selectedWords.Count} zadaniami.";

            return await CallGroqApiAsync(prompt, selectedWords.Count);
        }

        // =====================================================
        // GENEROWANIE ZADAŃ GRAMATYCZNYCH
        // =====================================================

        public async Task<List<ExerciseTaskModel>>
            GenerateGrammarTasksAsync(
                int grammarId,
                int count = 5)
        {
            if (count <= 0)
                throw new ArgumentException(
                    "Liczba zadań musi być większa od zera.");

            string grammarTopic =
                _databaseService.GetGrammarTopicName(grammarId);

            if (string.IsNullOrWhiteSpace(grammarTopic))
                return new List<ExerciseTaskModel>();

            string prompt = $@"
Jesteś profesjonalnym nauczycielem języka angielskiego.

Przygotuj dokładnie {count} różnorodnych zadań testowych typu fill in the blank.

ZAGADNIENIE GRAMATYCZNE:
{grammarTopic}

ZASADY:

1. Zdania muszą być napisane po angielsku.
2. Luka musi być oznaczona jako _____.
3. Luka musi wymagać użycia pełnej formy gramatycznej, a nie tylko pojedynczego słowa.
4. Możesz podać bezokolicznik czasownika w nawiasie.
5. Opcje muszą zawierać dokładnie 4 różne odpowiedzi.
6. Opcje muszą być w języku angielskim.
7. Dokładnie jedna odpowiedź musi być poprawna.
8. PoprawnaOdpowiedz musi zawierać dokładny tekst poprawnej opcji z tablicy Opcje.
9. Wyjasnienie musi być napisane po polsku i wyjaśniać zastosowaną konstrukcję gramatyczną.
10. Dystraktory powinny przedstawiać typowe błędy gramatyczne.
11. Nie powtarzaj zdań ani pytań.

12. Nie twórz odpowiedzi o takim samym znaczeniu.

Zabronione przykłady:
- don't like / do not like
- doesn't work / does not work
- can't swim / cannot swim
- won't go / will not go
- big / large
- begin / start

13. Nie używaj synonimów jako osobnych odpowiedzi.
14. Nie używaj skróconych i pełnych form tego samego wyrażenia jako różnych opcji.
15. Każda odpowiedź musi być jednoznaczna.
16. Tylko jedna odpowiedź może być poprawna w danym kontekście.

17. KRYTYCZNA ZASADA DOTYCZĄCA LUKI I CZASOWNIKÓW:

- Nie powtarzaj tego samego czasownika w jednym zdaniu.
- Nie powtarzaj operatora ani czasownika pomocniczego w konstrukcji, która tworzy błąd.
- Nie umieszczaj przed luką operatora lub części konstrukcji, która znajduje się również w poprawnej odpowiedzi.
- Jeśli odpowiedź to ""will arrive"", luka musi zastępować całą konstrukcję ""will arrive"".
- Jeśli odpowiedź to ""has finished"", luka musi zastępować całą konstrukcję ""has finished"".
- Jeśli odpowiedź to ""is working"", luka musi zastępować całą konstrukcję ""is working"".
- Nie dziel konstrukcji gramatycznej pomiędzy zdanie i lukę.
- Cała poprawna konstrukcja musi znajdować się w luce.

PRZYKŁADY POPRAWNE:

Zdanie: They _____ tomorrow.
Odpowiedź: will arrive.

Zdanie: She _____ her homework.
Odpowiedź: has finished.

Zdanie: He _____ now.
Odpowiedź: is working.

PRZYKŁADY NIEPOPRAWNE:

Zdanie: They will _____ tomorrow.
Odpowiedź: will arrive.

Zdanie: She has _____ her homework.
Odpowiedź: has finished.

Zdanie: He is _____ now.
Odpowiedź: is working.

18. OBOWIĄZKOWA KONTROLA:

1. Wstaw poprawną odpowiedź w miejsce _____.
2. Sprawdź, czy konstrukcja nie została powtórzona.
3. Sprawdź, czy cała konstrukcja gramatyczna znajduje się w luce.
4. Sprawdź, czy zdanie jest naturalne i gramatyczne.
5. Jeśli zdanie zawiera błąd, popraw zadanie.

Zwróć dokładnie {count} różnych zadań.";

            return await CallGroqApiAsync(prompt, count);
        }

        // =====================================================
        // NORMALIZACJA ODPOWIEDZI
        // =====================================================

        private static string NormalizeOptionForComparison(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.ToLowerInvariant().Trim();

            var replacements = new Dictionary<string, string>
            {
                { @"\bcan't\b", "can not" },
                { @"\bcannot\b", "can not" },
                { @"\bwon't\b", "will not" },
                { @"\bshan't\b", "shall not" },

                { @"\bdon't\b", "do not" },
                { @"\bdoesn't\b", "does not" },
                { @"\bdidn't\b", "did not" },

                { @"\bisn't\b", "is not" },
                { @"\baren't\b", "are not" },
                { @"\bwasn't\b", "was not" },
                { @"\bweren't\b", "were not" },

                { @"\bhaven't\b", "have not" },
                { @"\bhasn't\b", "has not" },
                { @"\bhadn't\b", "had not" },

                { @"\bwouldn't\b", "would not" },
                { @"\bshouldn't\b", "should not" },
                { @"\bcouldn't\b", "could not" },
                { @"\bmustn't\b", "must not" },

                { @"\bi'm\b", "i am" },
                { @"\byou're\b", "you are" },
                { @"\bwe're\b", "we are" },
                { @"\bthey're\b", "they are" },

                { @"\bhe's\b", "he is" },
                { @"\bshe's\b", "she is" },
                { @"\bit's\b", "it is" },

                { @"\bi've\b", "i have" },
                { @"\byou've\b", "you have" },
                { @"\bwe've\b", "we have" },
                { @"\bthey've\b", "they have" },

                { @"\bi'll\b", "i will" },
                { @"\byou'll\b", "you will" },
                { @"\bhe'll\b", "he will" },
                { @"\bshe'll\b", "she will" },
                { @"\bwe'll\b", "we will" },
                { @"\bthey'll\b", "they will" }
            };

            foreach (var replacement in replacements)
            {
                text = Regex.Replace(
                    text,
                    replacement.Key,
                    replacement.Value,
                    RegexOptions.IgnoreCase);
            }

            text = Regex.Replace(text, @"[^\w\s]", "");
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        // =====================================================
        // WALIDACJA ZDANIA PO WSTAWIENIU ODPOWIEDZI
        // =====================================================

        private static void ValidateCompletedSentence(
            string sentence,
            string correctAnswer)
        {
            if (string.IsNullOrWhiteSpace(sentence) ||
                string.IsNullOrWhiteSpace(correctAnswer))
            {
                throw new Exception(
                    "Zdanie lub poprawna odpowiedź jest pusta.");
            }

            // Wymagamy dokładnie jednej luki.
            int blankCount = Regex.Matches(
                sentence,
                Regex.Escape("_____")).Count;

            if (blankCount != 1)
            {
                throw new Exception(
                    "Zdanie musi zawierać dokładnie jedną lukę _____.");
            }

            string completedSentence = sentence.Replace(
                "_____",
                correctAnswer);

            string normalized = NormalizeOptionForComparison(
                completedSentence);

            // Sprawdzamy identyczne słowa występujące bezpośrednio obok siebie.
            // Przykład błędu: They will will arrive tomorrow.
            var words = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < words.Length; i++)
            {
                if (words[i] == words[i - 1])
                {
                    throw new Exception(
                        $"Wykryto powtórzone słowo '{words[i]}'. " +
                        $"Niepoprawne zdanie: {completedSentence}");
                }
            }

            // Sprawdzamy również powtórzenia operatora na granicy luki.
            // Nie odrzucamy automatycznie zdań, w których ten sam operator
            // występuje w dwóch niezależnych częściach zdania.
            string[] auxiliaryVerbs =
            {
                "will", "would", "shall", "should",
                "can", "could", "may", "might", "must",
                "have", "has", "had",
                "am", "is", "are", "was", "were",
                "do", "does", "did"
            };

            string beforeBlank = sentence
                .Split(new[] { "_____" }, StringSplitOptions.None)[0];

            string afterBlank = sentence
                .Split(new[] { "_____" }, StringSplitOptions.None)[1];

            string normalizedBefore =
                NormalizeOptionForComparison(beforeBlank);

            string normalizedAnswer =
                NormalizeOptionForComparison(correctAnswer);

            string normalizedAfter =
                NormalizeOptionForComparison(afterBlank);

            foreach (string auxiliary in auxiliaryVerbs)
            {
                bool answerStartsWithAuxiliary =
                    Regex.IsMatch(
                        normalizedAnswer,
                        @"^" + auxiliary + @"\b");

                bool answerEndsWithAuxiliary =
                    Regex.IsMatch(
                        normalizedAnswer,
                        @"\b" + auxiliary + @"$");

                bool beforeEndsWithAuxiliary =
                    Regex.IsMatch(
                        normalizedBefore,
                        @"\b" + auxiliary + @"$");

                bool afterStartsWithAuxiliary =
                    Regex.IsMatch(
                        normalizedAfter,
                        @"^" + auxiliary + @"\b");

                if (beforeEndsWithAuxiliary &&
                    answerStartsWithAuxiliary)
                {
                    throw new Exception(
                        $"Operator '{auxiliary}' został powtórzony " +
                        "przed luką i w poprawnej odpowiedzi. " +
                        $"Niepoprawne zdanie: {completedSentence}");
                }

                if (answerEndsWithAuxiliary &&
                    afterStartsWithAuxiliary)
                {
                    throw new Exception(
                        $"Operator '{auxiliary}' został powtórzony " +
                        "w odpowiedzi i po luce. " +
                        $"Niepoprawne zdanie: {completedSentence}");
                }
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new Exception(
                    "Zdanie po wstawieniu odpowiedzi jest puste.");
            }
        }

        // =====================================================
        // WALIDACJA ODPOWIEDZI
        // =====================================================

        private static void ValidateOptions(
            List<string> optionValues,
            string correctAnswer)
        {
            if (optionValues == null || optionValues.Count != 4)
            {
                throw new Exception(
                    "Każde zadanie musi mieć dokładnie 4 opcje.");
            }

            if (optionValues.Any(
                x => string.IsNullOrWhiteSpace(x)))
            {
                throw new Exception(
                    "Opcje nie mogą być puste.");
            }

            var normalizedOptions = optionValues
                .Select(NormalizeOptionForComparison)
                .ToList();

            if (normalizedOptions.Distinct(
                StringComparer.OrdinalIgnoreCase).Count() != 4)
            {
                throw new Exception(
                    "Wykryto odpowiedzi o takim samym znaczeniu " +
                    "lub identycznym zapisie.");
            }

            if (!optionValues.Contains(
                correctAnswer,
                StringComparer.Ordinal))
            {
                throw new Exception(
                    "Poprawna odpowiedź nie występuje w tablicy Opcje.");
            }
        }

        // =====================================================
        // ODCZYT I WALIDACJA ODPOWIEDZI MODELU
        // =====================================================

        private static List<ExerciseTaskModel> ParseAndValidateResponse(
            string responseString,
            int expectedCount)
        {
            using var document = JsonDocument.Parse(responseString);

            var root = document.RootElement;

            if (!root.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0)
            {
                throw new Exception(
                    "Groq nie zwrócił żadnej odpowiedzi.");
            }

            var message = choices[0].GetProperty("message");

            if (message.TryGetProperty("refusal", out var refusal))
            {
                throw new Exception(
                    "Model odmówił wygenerowania zadań: " +
                    refusal.GetString());
            }

            if (!message.TryGetProperty(
                "content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.String)
            {
                throw new Exception(
                    "Model nie zwrócił treści JSON.");
            }

            string rawText = contentElement.GetString();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                throw new Exception(
                    "Odpowiedź modelu jest pusta.");
            }

            using var jsonDocument = JsonDocument.Parse(rawText);

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
                    if (!task.TryGetProperty(field, out var value))
                    {
                        throw new Exception(
                            $"Brak wymaganego pola: {field}");
                    }

                    if (field == "Opcje")
                    {
                        if (value.ValueKind != JsonValueKind.Array)
                        {
                            throw new Exception(
                                "Pole Opcje musi być tablicą.");
                        }
                    }
                    else if (value.ValueKind != JsonValueKind.String)
                    {
                        throw new Exception(
                            $"Pole {field} musi być tekstem.");
                    }
                }

                var options = task.GetProperty("Opcje");

                var optionValues = options
                    .EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String
                        ? x.GetString()
                        : null)
                    .ToList();

                string correctAnswer = task
                    .GetProperty("PoprawnaOdpowiedz")
                    .GetString();

                ValidateOptions(
                    optionValues,
                    correctAnswer);

                string sentence = task
                    .GetProperty("TrescZdania")
                    .GetString();

                ValidateCompletedSentence(
                    sentence,
                    correctAnswer);

                string explanation = task
                    .GetProperty("Wyjasnienie")
                    .GetString();

                if (string.IsNullOrWhiteSpace(sentence) ||
                    string.IsNullOrWhiteSpace(explanation))
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

        // =====================================================
        // KOMUNIKACJA Z GROQ API + AUTOMATYCZNE PONAWIANIE
        // =====================================================

        private async Task<List<ExerciseTaskModel>>
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

            string previousErrors = "";

            // Wykonujemy maksymalnie 3 próby.
            for (int attempt = 1;
                 attempt <= MaxGenerationAttempts;
                 attempt++)
            {
                try
                {
                    string currentPrompt = prompt;

                    // Jeśli poprzednia próba się nie udała,
                    // przekazujemy modelowi informację o błędach.
                    if (!string.IsNullOrWhiteSpace(previousErrors))
                    {
                        currentPrompt += $@"

=====================================================
POPRAW BŁĘDY Z POPRZEDNIEJ PRÓBY
=====================================================

Poprzednia wygenerowana odpowiedź nie przeszła walidacji.

Wykryte błędy:
{previousErrors}

Wygeneruj cały zestaw zadań ponownie.

OBOWIĄZKOWO:
- Usuń wszystkie wykryte błędy.
- Sprawdź każde zadanie osobno.
- Wstaw poprawną odpowiedź w miejsce _____.
- Sprawdź całe zdanie po wstawieniu odpowiedzi.
- Nie powtarzaj operatorów na granicy luki.
- Zachowaj dokładną wymaganą liczbę zadań.
- Nie zwracaj poprzedniej błędnej wersji.
- Zwróć wyłącznie poprawny JSON zgodny ze schematem.";
                    }

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
                                    "zadania edukacyjne. Każde zadanie musi mieć " +
                                    "dokładnie jedną poprawną odpowiedź. " +
                                    "Nigdy nie generuj synonimów ani równoważnych " +
                                    "form jako osobnych opcji. " +
                                    "Nie powtarzaj operatorów na granicy luki. " +
                                    "Poprawna odpowiedź musi zastępować całą " +
                                    "konstrukcję gramatyczną wymaganą w luce. " +
                                    "Nie umieszczaj części poprawnej odpowiedzi " +
                                    "poza luką. " +
                                    "Zawsze sprawdzaj całe zdanie po wstawieniu " +
                                    "odpowiedzi. " +
                                    "Zawsze zwracasz dane zgodne z wymaganym schematem."
                            },

                            new
                            {
                                role = "user",
                                content = currentPrompt
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

                    // Odczytujemy odpowiedź i sprawdzamy wszystkie warunki.
                    var result = ParseAndValidateResponse(
                        responseString,
                        expectedCount);

                    // Jeśli dotarliśmy tutaj, zadania są poprawne.
                    return result;
                }
                catch (Exception ex)
                {
                    previousErrors = ex.Message;

                    // Jeśli wykorzystaliśmy wszystkie próby,
                    // zwracamy końcowy błąd.
                    if (attempt == MaxGenerationAttempts)
                    {
                        throw new Exception(
                            $"Nie udało się wygenerować poprawnych zadań " +
                            $"po {MaxGenerationAttempts} próbach.\n\n" +
                            $"Ostatni wykryty błąd:\n{previousErrors}",
                            ex);
                    }

                    // W przeciwnym razie pętla wykona kolejną próbę.
                }
            }

            throw new Exception(
                "Nie udało się wygenerować poprawnych zadań.");
        }
    }
}
