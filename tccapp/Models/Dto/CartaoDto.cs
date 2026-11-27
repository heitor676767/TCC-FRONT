using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models.Dto
{
    public class CartaoDto
    {
        public Guid Id { get; set; }
        public string Bandeira { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string UltimosDigitos { get; set; } = string.Empty;
        public string NomeTitular { get; set; } = string.Empty;
        public string Validade { get; set; } = string.Empty;

        public string TituloExibicao => $"{Bandeira} - {Tipo}";
        public string NumeroMascarado => $"********{UltimosDigitos}";
        public string IconeBandeira => "logo_cartao_padrao.png";
    }
}
