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

        // Parametry algorytmu SM-2
        public double EasinessFactor { get; set; } = 2.5; // wspolczynnik_latwosci (EF)
        public int Interval { get; set; } = 0;            // odstep_powtorek (I)
        public int Repetitions { get; set; } = 0;         // liczba_poprawnych_powtorek (N)
    }
}