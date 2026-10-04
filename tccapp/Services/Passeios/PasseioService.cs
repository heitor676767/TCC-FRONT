using tccapp.Models;

namespace tccapp.Services.Passeios
{
    public class PasseioService : Request
    {
        private readonly Request _request;
        private const string apiUrlBase = "https://api-tcc-eucbhub2chhgemgy.brazilsouth-01.azurewebsites.net/Passeio";
        private readonly string _token;

        public PasseioService(string token)
        {
            _request = new Request();
            _token = token;
        }

        // Pede um passeio pra um petwalker específico. Duracao em minutos.
        public async Task<Passeio> SolicitarAsync(
            string rga, string cpfPetwalker, int duracao,
            decimal latitude, decimal longitude, string cep, string numero)
        {
            var dto = new
            {
                Rga = rga,
                CpfPetwalker = cpfPetwalker,
                DataPass = DateTime.Now,
                Duracao = duracao,
                Latitude = latitude,
                Longitude = longitude,
                Cep = cep,
                Numero = numero
            };

            return await _request.PostAsync<object, Passeio>(apiUrlBase + "/Solicitar", dto, _token);
        }

        public async Task<List<Passeio>> GetMeusAsync()
        {
            return await _request.GetAsync<List<Passeio>>(apiUrlBase + "/Meus", _token);
        }
    }
}
