using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models
{
    public class LocalizacaoPasseio
    {
        public int IdPasseio { get; set; } // PK e FK -> Passeio.IdPasseio
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Cep { get; set; }
        public string Numero { get; set; }

        public Passeio Passeio { get; set; } // navegação
    }
}
