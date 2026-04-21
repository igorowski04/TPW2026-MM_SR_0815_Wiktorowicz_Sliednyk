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
using System.Reactive;
using System.Windows.Input;
using TP.ConcurrentProgramming.Presentation.Model;
using TP.ConcurrentProgramming.Presentation.ViewModel.MVVMLight;
using ModelIBall = TP.ConcurrentProgramming.Presentation.Model.IBall;

namespace TP.ConcurrentProgramming.Presentation.ViewModel
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        #region ctor
            public MainWindowViewModel() : this(null)
            { }

            internal MainWindowViewModel(ModelAbstractApi modelLayerAPI)
            {
                ModelLayer = modelLayerAPI ?? ModelAbstractApi.CreateModel();
                Observer = ModelLayer.Subscribe<ModelIBall>(x => Balls.Add(x));

                StartCommand = new RelayCommand(StartSimulation);
            }
        #endregion ctor

        #region public API
                
        public string NumberOfBallsInput
        {
            get { return numberOfBallsInput; }
            set
            {
                if (numberOfBallsInput != value)
                {
                    numberOfBallsInput = value;
                    RaisePropertyChanged();
                }
            }
        }
        
        public ICommand StartCommand { get; }
        
        public ObservableCollection<ModelIBall> Balls { get; } = new ObservableCollection<ModelIBall>();
        
        #endregion public API

        #region IDisposable
            protected virtual void Dispose(bool disposing)
            {
                if (!Disposed)
                {
                    if (disposing)
                    {
                        Balls.Clear();
                        Observer.Dispose();
                        ModelLayer.Dispose();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    Disposed = true;
                }
            }

            public void Dispose()
            {
                if (Disposed)
                    return;
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        #endregion IDisposable

        #region private
            private IDisposable Observer;
            private ModelAbstractApi ModelLayer;
            private bool Disposed = false;
            private string numberOfBallsInput = "5";

        private void StartSimulation()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));

            if (int.TryParse(NumberOfBallsInput, out int numberOfBalls) && numberOfBalls > 0)
            {
                Balls.Clear();

                double boardWidth = 700;
                double boardHeight = 400;

                ModelLayer.Start(numberOfBalls, boardWidth, boardHeight);
            }
        }
        #endregion private
    }
}