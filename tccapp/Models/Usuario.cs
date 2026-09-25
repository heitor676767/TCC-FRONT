using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace tccapp.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Cpf { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;
        public string? StatusUser { get; set; }
        public string Telefone { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public string? Foto { get; set; }
        public DateTime UltimoLogin { get; set; }
        public DateTime DataCadastro { get; set; }
        public string PasswordString { get; set; } = string.Empty;
        public string? Token { get; set; } = string.Empty;

        // Opcional: útil se você for diferenciar Dono/Petwalker na tela inicial
        //public PetwalkerPerfil? PetwalkerPerfil { get; set; }
    }
}