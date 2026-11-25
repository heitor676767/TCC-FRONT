using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using tccapp.Models.Dto;

namespace tccapp.Services.Sessoes
{
    public class SessoesService : Request 
    {
        private readonly Request _request;
        private const string apiUrlBase = "https://api-tcc-eucbhub2chhgemgy.brazilsouth-01.azurewebsites.net/Sessoes";
        private readonly string _token;

        public SessoesService(string token)
        {
            _request = new Request();
            _token = token;
        }

        public async Task<ObservableCollection<SessaoDto>> ListarSessoesAsync()
        {
            return await _request.GetAsync<ObservableCollection<SessaoDto>>(apiUrlBase, _token);
        }

        public async Task RevogarSessaoAsync(Guid id)
        {
            await _request.DeleteNoContentAsync($"{apiUrlBase}/{id}", _token);
        }

        public async Task RevogarTodasAsync()
        {
            await _request.DeleteNoContentAsync(apiUrlBase, _token);
        }
    }
}
