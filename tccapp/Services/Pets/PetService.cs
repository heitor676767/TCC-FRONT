using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Linq.Expressions;
using tccapp.Models;
namespace tccapp.Services.Pets
{
    public class PetService : Request
    {
        private readonly Request _request;
        private const string apiUrlBase = "https://api-tcc-eucbhub2chhgemgy.brazilsouth-01.azurewebsites.net/Pet";

        public PetService()
        {
            _request = new Request();
        }

    }
}
