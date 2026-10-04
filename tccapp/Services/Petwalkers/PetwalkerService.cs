using System.Collections.ObjectModel;
using tccapp.Models;

namespace tccapp.Services.Petwalkers
{
    public class PetwalkerService : Request
    {
        private readonly Request _request;
        private const string apiUrlBase = "https://api-tcc-eucbhub2chhgemgy.brazilsouth-01.azurewebsites.net/Petwalker";
        private readonly string _token;

        public PetwalkerService(string token)
        {
            _request = new Request();
            _token = token;
        }

        // Busca petwalkers disponíveis dentro de um raio (km) a partir da localização do usuário.
        public async Task<ObservableCollection<PetwalkerPerfil>> GetDisponiveisAsync(double lat, double lng, double raioKm = 10)
        {
            string url = $"{apiUrlBase}/Disponiveis?lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                         $"&lng={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                         $"&raioKm={raioKm.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            return await _request.GetAsync<ObservableCollection<PetwalkerPerfil>>(url, _token);
        }
    }
}
