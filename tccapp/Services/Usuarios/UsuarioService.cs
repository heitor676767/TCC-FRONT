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
    }
}