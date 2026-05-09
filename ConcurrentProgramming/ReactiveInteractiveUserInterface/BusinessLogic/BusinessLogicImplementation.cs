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
using System.Threading;
using System.Threading.Tasks;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;
using DataBall = TP.ConcurrentProgramming.Data.IBall;

namespace TP.ConcurrentProgramming.BusinessLogic
{
    internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
    {
        private bool Disposed = false;
        private readonly UnderneathLayerAPI layerBellow;

        // ===============
        // -=- ZMIENNE -=-
        // ===============
        private Timer? MoveTimer; // Nasz silnik czasowy
        private List<DataBall> _dataBalls = new(); // Lista kul pobranych z bazy danych
        private double _boardWidth;
        private double _boardHeight;
        
        // Kontroler do zatrzymywania wielu wątków na raz
        private CancellationTokenSource? _cancaleTokenSource;
        // To jest sekcja krytyczna, któa zabezpiecza przed wyścigiem (Race Condition)
        private readonly object _collisionLock = new object();


        #region ctor
        public BusinessLogicImplementation() : this(null) { }

        internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
        {
            layerBellow = underneathLayer ?? UnderneathLayerAPI.GetDataLayer();
        }
        #endregion ctor

        #region BusinessLogicAbstractAPI

        public override void Dispose()
        {
            if (Disposed) return;
            // W tym miejscu zatrzymywane są wszystkie asynchroniczne zadania
            _cancaleTokenSource?.Cancel();
            _cancaleTokenSource?.Dispose();

            layerBellow.Dispose();
            Disposed = true;
        }

        public override void Start(int numberOfBalls, double width, double height, Action<IPosition, IBall> upperLayerHandler)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BusinessLogicImplementation));

            _boardWidth = width;
            _boardHeight = height;
            _dataBalls.Clear();

            // Odpytujemy warstwę danych (bazę) o wygenerowanie kul
            layerBellow.Start(numberOfBalls, width, height, (startingPosition, dataBall) =>
            {
                _dataBalls.Add(dataBall);

                // Przekazujemy nową biznesową kulę wyżej do UI
                upperLayerHandler(new Position(startingPosition.X, startingPosition.Y), new BusinessBall(dataBall));
            });
            
            // W tym miejscu uruchomiona zostaje współbieżność.
            // Każda kula wędruje do swojego własnego, żyjącego w tle wątku 
            _cancaleTokenSource = new CancellationTokenSource();
            foreach (var ball in _dataBalls)
            {
                Task.Run(() => MoveBallAsync(ball, _cancaleTokenSource.Token))
            }
        }

        #endregion BusinessLogicAbstractAPI

        #region Physics Engine (Private)

        // Pętla asynchroniczna życia kuli
        private async Task MoveBallAsync(DataBall ball, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {   
                // Pojedyńczy wątek zatrzymujemy na 20 ms, żeby nie blokować reszty programu
                await Task.Delay(20, token); 

                // SEKCJA KRYTYCZNA: W danym momencie tylko jedna kula może aktualizować wektory i badać kolizje
                lock (_collisionLock)
                {
                    double newX = ball.Position.X + ball.Velocity.X;
                    double newY = ball.Position.Y + ball.Velocity.Y;
                    double newVx = ball.Velocity.X;
                    double newVy = ball.Velocity.Y;
                    
                    // Odbicie od lewej / prawej ściany
                    if (newX <= 0)
                    {
                        newX = 0;
                        newVx = -newVx;
                    } 
                    else if (newX + (ball.Radius * 2) >= _boardWidth)
                    {
                        newX = _boardWidth - (ball.Radius * 2);
                        newVx = -newVx;
                    }

                    // Odbicie od górnej / dolnej ściany
                    if (newY <= 0)
                    {
                        newY = 0;
                        newVy = -newVy;
                    } 
                    else if (newY + (ball.Radius * 2) >= _boardHeight)
                    {
                        newY = _boardHeight - (ball.Radius * 2);
                        newVy = -newVy;
                    }

                    ball.Velocity = new DataVector(newVx, newVy);
                    ball.Position = new DataVector(newX, newY);

                    foreach (var otherball in _dataBalls)
                    {
                        if (otherball == ball) continue;
                        CheckCollision(ball, otherball);
                    }
                }
            }
        }


        // Fizyka 2D z uwzględnieniem masy.
        private void CheckCollision(DataBall b1, DataBall b2)
        {
            double dx = b2.Position.X - b1.Position.X;
            double dy = b2.Position.Y - b1.Position.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance == 0) return;

            if (distance <= b1.Radius + b2.Radius)
            {
                double nx = dx / distance;
                double ny = dy / distance;

                double relativeVelocityX = b2.Velocity.X - b1.Velocity.X;
                double relativeVelocityY = b2.Velocity.Y - b1.Velocity.Y;

                double dotProduct = relativeVelocityX * nx + relativeVelocityY * ny;

                if (dotProduct > 0) return;

                double impulse = (2.0 * dotProduct) / (b1.Mass + b2.Mass);

                double newVx1 = b1.Velocity.X + (impulse * b2.Mass * nx);
                double newVy1 = b1.Velocity.Y + (impulse * b2.Mass * ny);

                double newVx2 = b2.Velocity.X - (impulse * b1.Mass * nx);
                double newVy2 = b2.Velocity.Y - (impulse * b1.Mass * ny);

                b1.Velocity = new DataVector(newVx1, newVy1);
                b2.Velocity = new DataVector(newVx2, newVy2);
            }
        }

        #endregion Physics Engine
        #region TestingInfrastructure
            [Conditional("DEBUG")]
            internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
            {
                returnInstanceDisposed(Disposed);
            }
        #endregion TestingInfrastructure
        // ===============
        // -=- ADAPTER -=-
        // ===============
        // Mały adapter "w locie", który pozwala logice wysyłać wektory do warstwy danych, 
        // zachowując zgodność z interfejsem IVector, na który patrzy baza danych.
        private record DataVector(double X, double Y) : TP.ConcurrentProgramming.Data.IVector;
    }
}