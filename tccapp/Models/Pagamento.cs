using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models
{
    public class Pagamento
    {
        public int IdTransacao { get; set; } // int, chave primária, identity

        public string MtdPgmt { get; set; }

        public string StatusPgmt { get; set; }

        public decimal Valor { get; set; } // decimal(10,2)

        public DateTime DataPgmt { get; set; } = DateTime.Now; // default getdate()

        public int IdPasseio { get; set; } // FK -> Passeio.IdPasseio
    }
}
