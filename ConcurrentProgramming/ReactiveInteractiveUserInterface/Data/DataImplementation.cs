//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Collections.Generic;
using System.Diagnostics;

// Usunęliśmy z pliku Timer i obliczanie kolizji ponieważ warstwa danych ma służyć tylko do przechowywania informacji
// Wcześniej dodatkowo uruchamiała Timer i obliczała kolizję dla kul

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {

        // ===============
        // -=- Zmienne -=-
        // ===============
        private bool Disposed = false;
        private Random RandomGenerator = new();
        private List<Ball> BallsList = [];
        private readonly double BallRadius = 15.0;

        #region ctor
            // Domyślny konstruktor
            public DataImplementation() { }
        #endregion ctor

        #region DataAbstractAPI
            // - Funckja inicjalizuje warstwę danych jako fabrykę. 
            // - Tworzy określoną liczbę kul o losowych pozycjach i prędkościach 
            // - width i height jako wymiary planszy
            // - upperLayerHandler: funkcja, która przekaże nowo otworzoną kulę piętro wyżej
            public override void Start(int numberOfBalls, double width, double height, Action<IVector, IBall> upperLayerHandler)
            {   
                // Jeśli warstwa danych już została zniszona - nie będzie można wystartować funkcji
                if (Disposed)
                    throw new ObjectDisposedException(nameof(DataImplementation));
                if (upperLayerHandler == null)
                    throw new ArgumentNullException(nameof(upperLayerHandler));

                BallsList.Clear();
                
                // Pętla generująca wybraną liczbę kul o losowych koordynatach i losowej prędkości
                for (int i = 0; i < numberOfBalls; i++)
                {
                    double startX = RandomGenerator.NextDouble() * (width - 2 * BallRadius);
                    double startY = RandomGenerator.NextDouble() * (height - 2 * BallRadius);
                    Vector startingPosition = new(startX, startY);

                    //double vx = (RandomGenerator.NextDouble() - 0.5) * 12;
                    double vx = 1000;
                    //double vy = (RandomGenerator.NextDouble() - 0.5) * 12;
                    double vy = 1000;
                    Vector initialVelocity = new(vx, vy);
                    
                    
                    Ball newBall = new(startingPosition, initialVelocity, BallRadius, 1.0); // 1.0 to domyślna masa 
                    BallsList.Add(newBall);
                    
                    // Tutaj trafia informacja do wyższej warstwy o tym, że powstała nowa kula
                    upperLayerHandler(startingPosition, newBall);
                }
            }
        #endregion DataAbstractAPI

        #region IDisposable
            // metoda zwalniająca zasoby. Czyści listę kul
            protected virtual void Dispose(bool disposing)
            {
                if (!Disposed)
                {
                    if (disposing)
                    {
                        BallsList.Clear();
                    }
                    Disposed = true;
                }
            }
            
            // metoda wywoływane przy zamywaniu aplikacji. Zwalnia pamięć
            public override void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        #endregion IDisposable

        #region TestingInfrastructure
            [Conditional("DEBUG")]
            internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
            {
                returnBallsList(BallsList);
            }

            [Conditional("DEBUG")]
            internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
            {
                returnNumberOfBalls(BallsList.Count);
            }

            [Conditional("DEBUG")]
            internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
            {
                returnInstanceDisposed(Disposed);
            }
        #endregion TestingInfrastructure
    }
}