using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearnLanguages.Models
{
    public class ExerciseTaskModel
    {
        public string TrescZdania { get; set; } // Zdanie z luką oznaczoną jako "_____"
        public List<string> Opcje { get; set; }  // Lista 4 odpowiedzi (np. A, B, C, D)
        public string PoprawnaOdpowiedz { get; set; } // Dokładny string pasujący do poprawnej opcji
        public string Wyjasnienie { get; set; } // Krótkie wyjaśnienie po polsku
    }
}
