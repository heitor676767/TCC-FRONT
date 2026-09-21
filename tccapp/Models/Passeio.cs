using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models
{
    public class Passeio
    {
        public int IdPasseio { get; set; } // int, chave primária, identity

        public string StatusPass { get; set; }

        public DateTime DataPass { get; set; } // date

        public int Duracao { get; set; }

        public string Rga { get; set; } // char(7), FK -> Pet.Rga

        public string CpfPetwalker { get; set; } // char(11), FK -> PetwalkerPerfil.Cpf
        //Navegacao
        public Pet Pet { get; set; } = null!;
        public PetwalkerPerfil PetwalkerPerfil { get; set; } = null!;
        public ICollection<Transacao> Transacoes { get; set; } = new List<Transacao>();
        public LocalizacaoPasseio LocalizacaoPasseio { get; set; } = null!;
    }
}
