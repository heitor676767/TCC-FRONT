using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Linq.Expressions;
using tccapp.Models;

namespace tccapp.Services.Usuarios
{
    public class UsuarioService : Request
    {
        private readonly Request _request;
        private const string apiUrlBase = "https://api-tcc-eucbhub2chhgemgy.brazilsouth-01.azurewebsites.net/Usuarios";

        public UsuarioService()
        {
            _request = new Request();
        }

        public async Task<Usuario> PostRegistrarUsuarioAsync(Usuario u)
        {
            string urlComplementar = "/Registrar";
            u.Id = await _request.PostReturnIntAsync(apiUrlBase + urlComplementar, u, string.Empty);

            return u;
        }

        public async Task<Usuario> PostAutenticarUsuarioAsync(Usuario u)
        {
            string urlComplementar = "/Autenticar";
            u = await _request.PostAsync(apiUrlBase + urlComplementar, u, string.Empty);

            return u;
        }

        public async Task<bool> VerificarEmailAsync(string email)
        {
            string urlComplementar = $"/VerificarEmail?email={Uri.EscapeDataString(email)}";
            return await _request.GetAsync<bool>(apiUrlBase + urlComplementar, string.Empty);
        }

        public async Task<bool> VerificarCpfAsync(string cpf)
        {
            string urlComplementar = $"/VerificarCpf?cpf={Uri.EscapeDataString(cpf)}";
            return await _request.GetAsync<bool>(apiUrlBase + urlComplementar, string.Empty);
        }

        public async Task<bool> VerificarTelefoneAsync(string telefone)
        {
            string urlComplementar = $"/VerificarTelefone?telefone={Uri.EscapeDataString(telefone)}";
            return await _request.GetAsync<bool>(apiUrlBase + urlComplementar, string.Empty);
        }

        // Edita o perfil do usuário logado. CPF e Email não entram aqui de propósito
        // (a API não deixa mudar esses dois por esse endpoint).
        public async Task<Usuario> AtualizarUsuarioAsync(
            string nome, string telefone, string cep, string genero, string token)
        {
            var dto = new { Nome = nome, Telefone = telefone, Cep = cep, Genero = genero };
            return await _request.PutAsync<object, Usuario>(apiUrlBase, dto, token);
        }

        public async Task EsqueciSenhaAsync(string email)
        {
            string urlComplementar = "/EsqueciSenha";
            var dto = new { Email = email };
            await _request.PostAsync(apiUrlBase + urlComplementar, dto, string.Empty);
        }

        public async Task RedefinirSenhaAsync(string email, string codigo, string novaSenha)
        {
            string urlComplementar = "/RedefinirSenha";
            var dto = new { Email = email, Codigo = codigo, NovaSenha = novaSenha };
            await _request.PostAsync(apiUrlBase + urlComplementar, dto, string.Empty);
        }
    }
}