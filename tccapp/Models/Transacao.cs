using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models
{
    public class Transacao
    {
        public int IdTransacao { get; set; }
        public string MtdPgmt { get; set; }
        public string StatusPgmt { get; set; }
        public decimal Valor { get; set; }
        public DateOnly DataPgmt { get; set; }

        public int IdPasseio { get; set; } // FK


        public Passeio Passeio { get; set; } // navegação
    }
}
