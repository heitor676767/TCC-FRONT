using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using tccapp.Services.Petwalkers;

namespace tccapp.ViewModels
{
    public partial class HomeViewPetwalkerModel:ObservableObject
    {
        [ObservableProperty] private bool disponivel;
        [ObservableProperty] private bool carregando;
        public async Task AlterarDisponibilidadeAsync(bool novoStatus) 
        { 
            try { 
                Carregando = true; 
                string token = Preferences.Get("UsuarioToken", string.Empty); 
                if (string.IsNullOrWhiteSpace(token)) 
                { await Shell.Current.DisplayAlert("Erro", "Usuário não autenticado.", "OK"); 
                    return;
                }
                var service = new PetwalkerService(token); bool resultado = await service.AtualizarDisponibilidadeAsync(novoStatus);
                Disponivel = resultado; 
            } 
            catch (Exception ex) { 
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            } 
            finally 
            { 
                Carregando = false;
            } 
        }
    }
}
