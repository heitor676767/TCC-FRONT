using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.Models.Dto
{
    public class SessaoDto
    {
        public Guid Id { get; set; }
        public string NomeDispositivo { get; set; } = string.Empty;
        public DateTime UltimoAcesso { get; set; }
        public bool Atual { get; set; }

        public string TituloExibicao => Atual
            ? $"{NomeDispositivo} (Este aparelho)"
            : NomeDispositivo;

        public string UltimoAcessoFormatado =>
            $"{UltimoAcesso.ToLocalTime():dd/MM/yyyy, HH:mm} BRT";
    }
}
