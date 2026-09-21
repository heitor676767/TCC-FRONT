using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models
{
    public class Endereco
    {
        public int IdPasseio { get; set; } // int, chave primária E FK -> Passeio.IdPasseio

        public decimal Latitude { get; set; } // decimal(9,6)

        public decimal Longitude { get; set; } // decimal(9,6)

        public string Cep { get; set; } // char(8)

        public string Numero { get; set; }
    }
}
