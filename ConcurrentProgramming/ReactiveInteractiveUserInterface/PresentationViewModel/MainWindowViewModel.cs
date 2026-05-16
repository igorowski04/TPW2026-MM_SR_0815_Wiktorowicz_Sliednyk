//__________________________________________________________________________________________
//
//  Copyright 2024 Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and to get started
//  comment using the discussion panel at
//  https://github.com/mpostol/TP/discussions/182
//__________________________________________________________________________________________

using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TP.ConcurrentProgramming.Presentation.Model;
using TP.ConcurrentProgramming.Presentation.ViewModel.MVVMLight;
using ModelIBall = TP.ConcurrentProgramming.Presentation.Model.IBall;

namespace TP.ConcurrentProgramming.Presentation.ViewModel
{
    /// <summary>
    /// ViewModel - to nasz "Kelner". Pobiera zamówienia z Widoku (np. kliknięcie przycisku Start),
    /// przekazuje je do Kuchni (Modelu) i odnosi gotowe dania (Kule) z powrotem na ekran za pomocą ObservableCollection.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        // ============================
        // -=- ZMIENNE I USTAWIENIA -=-
        // ============================

        private IDisposable Observer;
        private ModelAbstractApi ModelLayer;
        private bool Disposed = false;
        private string numberOfBallsInput = "5";

        
        private readonly double _boardWidth = 700;
        private readonly double _boardHeight = 400;

        #region ctor

        public MainWindowViewModel() : this(null) { }

        internal MainWindowViewModel(ModelAbstractApi? modelLayerAPI)
        {
            // 1. Podpinamy Model (jeśli nie został podany z zewnątrz, tworzymy nowy)
            ModelLayer = modelLayerAPI ?? ModelAbstractApi.CreateModel();

            // 2. Wzorzec Obserwatora: Nasłuchujemy na nowe kule z Modelu. 
            // Jak tylko Model wygeneruje kulę, my automatycznie dodajemy ją do listy widocznej na ekranie.
            Observer = ModelLayer.Subscribe(x => Balls.Add(x));

            // 3. Podpinamy komendę kliknięcia przycisku do naszej metody StartSimulation
            StartCommand = new RelayCommand(StartSimulation);
        }

        #endregion ctor

        #region public API

        /// <summary>
        /// Właściwość podpięta (Binding) pod TextBoxa na ekranie, w którym użytkownik wpisuje ilość kul.
        /// </summary>
        public string NumberOfBallsInput
        {
            get { return numberOfBallsInput; }
            set
            {
                if (numberOfBallsInput != value)
                {
                    numberOfBallsInput = value;
                    RaisePropertyChanged(); // Informujemy widok, że tekst się zmienił
                }
            }
        }

        /// <summary>
        /// Komenda wywoływana przy kliknięciu przycisku "Start".
        /// </summary>
        public ICommand StartCommand { get; }

        /// <summary>
        /// Magiczna lista z WPF. Każda kula dodana do tej listy automatycznie i natychmiast 
        /// pojawia się na ekranie (w ItemsControl/Canvas).
        /// </summary>
        public ObservableCollection<ModelIBall> Balls { get; } = new ObservableCollection<ModelIBall>();

        public void UpdateMousePosition(double x, double y)
        {
            ModelLayer.UpdatePlayerPosition(x, y);
        }
        #endregion public API

        #region private

        private void StartSimulation()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));

            // Zabezpieczenie przed "głupotą użytkownika" - startujemy tylko, 
            // jeśli w TextBoxie wpisano poprawną liczbę całkowitą, która jest większa od 0.
            if (int.TryParse(NumberOfBallsInput, out int numberOfBalls) && numberOfBalls > 0)
            {
                // Czyścimy starą symulację na ekranie
                Balls.Clear();

                // Odpalamy logikę! Wysyłamy prośbę do modelu o wygenerowanie kul.
                ModelLayer.Start(numberOfBalls, _boardWidth, _boardHeight);
            }
        }

        #endregion private

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // Sprzątamy pamięć przy wyłączeniu
                    Balls.Clear();
                    Observer.Dispose();
                    ModelLayer.Dispose();
                }
                Disposed = true;
            }
        }

        public void Dispose()
        {
            if (Disposed) return;
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }
}