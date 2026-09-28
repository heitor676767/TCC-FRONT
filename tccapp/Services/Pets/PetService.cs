using System.Collections.ObjectModel;
using tccapp.Models;

namespace tccapp.Services.Pets
{
    public class PetService : Request
    {
        private readonly Request _request;
        private const string apiUrlBase = "https://api-tcc-eucbhub2chhgemgy.brazilsouth-01.azurewebsites.net/Pet";
        private readonly string _token;

        public PetService(string token)
        {
            _request = new Request();
            _token = token;
        }

        public async Task<int> PostRegistrarPetAsync(Pet p)
        {
            return await _request.PostReturnIntAsync(apiUrlBase + "/Registrar", p, _token);
        }

        public async Task<ObservableCollection<Pet>> GetMeusPetsAsync(string cpfDono)
        {
            string url = $"{apiUrlBase}/GetMeusPets?cpfDono={Uri.EscapeDataString(cpfDono)}";
            return await _request.GetAsync<ObservableCollection<Pet>>(url, _token);
        }
    }
}