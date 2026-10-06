using System;
using tccapp.Models;

namespace tccapp.Models
{
    // Espelha o PasseioDto que a API devolve em GET /Passeio/Meus e /Passeio/{id}.
    // Diferente de Passeio.cs (que espelha a entidade completa, com navegações
    // que não vêm preenchidas nessas respostas), esse já vem achatado.
    public class PasseioDto
    {
        public int IdPasseio { get; set; }
        public string StatusPass { get; set; } = string.Empty;
        public DateTime DataPass { get; set; }
        public int Duracao { get; set; }
        public DateTime? DataInicio { get; set; }

        public string PetRga { get; set; } = string.Empty;
        public string PetNome { get; set; } = string.Empty;

        public string DonoCpf { get; set; } = string.Empty;
        public string DonoNome { get; set; } = string.Empty;

        public string PetwalkerCpf { get; set; } = string.Empty;
        public string PetwalkerNome { get; set; } = string.Empty;

        public LocalizacaoInicialDto? Localizacao { get; set; }
    }

    // Ponto de encontro combinado na solicitação do passeio (lat/lng de onde ele começou).
    public class LocalizacaoInicialDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Cep { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
    }
}