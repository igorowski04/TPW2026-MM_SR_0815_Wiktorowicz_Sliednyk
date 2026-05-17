//__________________________________________________________________________________________
//
//  Copyright 2024 Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and to get started
//  comment using the discussion panel at
//  https://github.com/mpostol/TP/discussions/182
//__________________________________________________________________________________________

using System;
using System.Windows;
using System.Windows.Input;
using TP.ConcurrentProgramming.Presentation.ViewModel;

namespace TP.ConcurrentProgramming.PresentationView
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
      
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.DataContext is TP.ConcurrentProgramming.Presentation.ViewModel.MainWindowViewModel vm)
            {
                // Złapanie pozycji myszy względem okna
                var position = e.GetPosition(BillardTable);
                vm.UpdateMousePosition(position.X, position.Y);
            }
        }
    }
}