using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using tccapp.Models.Dto;

namespace tccapp.Services.Carteiras
{
    public class CartoesService:Request
    {
        private readonly Request _request;
        private const string apiUrlBase = "https://api-tcc-eucbhub2chhgemgy.brazilsouth-01.azurewebsites.net/Cartoes";
        private readonly string _token;

        public CartoesService(string token)
        {
            _request = new Request();
            _token = token;
        }

        public async Task<ObservableCollection<CartaoDto>> ListarCartoesAsync()
        {
            return await _request.GetAsync<ObservableCollection<CartaoDto>>(apiUrlBase, _token);
        }

        public async Task<CartaoDto> AdicionarCartaoAsync(object novoCartao)
        {
            return await _request.PostAsync<object, CartaoDto>(apiUrlBase, novoCartao, _token);
        }

        public async Task RemoverCartaoAsync(Guid id)
        {
            await _request.DeleteNoContentAsync($"{apiUrlBase}/{id}", _token);
        }
    }
}

