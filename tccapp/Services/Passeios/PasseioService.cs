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
        // Corrigido: a API responde com PasseioDto (igual GetMeusAsync), não a entidade
        // Passeio completa — o tipo antigo deixava tudo vindo vazio silenciosamente.
        public async Task<PasseioDto> SolicitarAsync(
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

            return await _request.PostAsync<object, PasseioDto>(apiUrlBase + "/Solicitar", dto, _token);
        }

        // Corrigido: a API devolve PasseioDto (campos achatados), não a entidade Passeio
        // inteira — usar o tipo errado aqui fazia PetNome/PetwalkerNome virem sempre vazios.
        public async Task<List<PasseioDto>> GetMeusAsync()
        {
            return await _request.GetAsync<List<PasseioDto>>(apiUrlBase + "/Meus", _token);
        }

        public async Task<PasseioDto> GetPorIdAsync(int idPasseio)
        {
            return await _request.GetAsync<PasseioDto>($"{apiUrlBase}/{idPasseio}", _token);
        }
    }
}