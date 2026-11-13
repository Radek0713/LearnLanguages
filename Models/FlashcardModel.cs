using System;

namespace LearnLanguages.Models
{
    public class FlashcardModel
    {
        // Potrzebne do późniejszych aktualizacji algorytmu SM-2
        public int TranslationId { get; set; }

        public string CategoryName { get; set; } // dział (np. Dziecko)
        public string TopicName { get; set; }    // temat (np. Plac zabaw)

        public string PolImageName { get; set; } // plik z tabeli 'slowko'
        public string EngImageName { get; set; } // plik z tabeli 'tlumaczenie'
    }
}