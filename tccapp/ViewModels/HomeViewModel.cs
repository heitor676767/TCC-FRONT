using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace tccapp.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {



        [RelayCommand]
        private async Task Home()
        {
            await Shell.Current.GoToAsync("Home");
        }
    }
}
