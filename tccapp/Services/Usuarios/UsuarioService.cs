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
        private const string apiUrlBase = "https://apitcc-g9hmd4fng7dhgeae.centralus-01.azurewebsites.net";
        public UsuarioService()
        {
            _request = new Request();
        }
        public async Task<Usuario> PostRegistrarUsuarioAsync(Usuario u) 
        {
            return null;
        }
        

    }
}
