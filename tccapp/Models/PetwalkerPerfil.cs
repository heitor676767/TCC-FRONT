using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models
{
    public class PetwalkerPerfil
    {
        public string Cpf { get; set; } // PK e FK -> Usuario.Cpf

        public bool Disponibilidade { get; set; } = false;

        public string AreaAtendimento { get; set; }

        // Navegação 1:1 com Usuario
        public Usuario Usuario { get; set; }

        // Navegação 1:N
        public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();

        public ICollection<Passeio> Passeios { get; set; } = new List<Passeio>();
    }
}
