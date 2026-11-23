using System.Collections.Generic;
using System.Threading.Tasks;
using LearnLanguages.Models;

namespace LearnLanguages.Services
{
    public interface ILLMService
    {
        Task<List<ExerciseTaskModel>> GenerateBatchTasksForWordsAsync(List<string> words, int count = 5);
        Task<List<ExerciseTaskModel>> GenerateGrammarTasksAsync(int grammarId, int count = 5);
    }
}