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
            MoveTimer?.Dispose();
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

            // Odpalamy silnik fizyczny: wywołuj metodę 'Move' co 20 milisekund
            MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(20));
        }

        #endregion BusinessLogicAbstractAPI

        #region Physics Engine (Private)

        // Główna pętla przeliczająca fizykę
        private void Move(object? state)
        {
            if (Disposed) return;

            // Zatrzymujemy timer na czas obliczeń, żeby klatki się na siebie nie nakładały
            MoveTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                // 1. Sprawdzamy zderzenia między kulami
                for (int i = 0; i < _dataBalls.Count; i++)
                {
                    for (int j = i + 1; j < _dataBalls.Count; j++)
                    {
                        CheckCollision(_dataBalls[i], _dataBalls[j]);
                    }
                }

                // 2. Przesuwamy kule i odbijamy od ścian
                foreach (var ball in _dataBalls)
                {
                    double newX = ball.Position.X + ball.Velocity.X;
                    double newY = ball.Position.Y + ball.Velocity.Y;
                    double newVx = ball.Velocity.X;
                    double newVy = ball.Velocity.Y;

                    // Odbicie od lewej/prawej ściany
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

                    // Odbicie od górnej/dolnej ściany
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

                    // Nadpisujemy stan w bazie danych korzystając z naszego DataVector
                    ball.Velocity = new DataVector(newVx, newVy);
                    ball.Position = new DataVector(newX, newY);
                }
            }
            finally
            {
                // Odpalamy timer na kolejne 20ms
                MoveTimer?.Change(20, Timeout.Infinite);
            }
        }

        // Skomplikowana matematyka zderzeń idealnie sprężystych
        private void CheckCollision(DataBall b1, DataBall b2)
        {
            double c1X = b1.Position.X + b1.Radius;
            double c1Y = b1.Position.Y + b1.Radius;
            double c2X = b2.Position.X + b2.Radius;
            double c2Y = b2.Position.Y + b2.Radius;

            double dx = c2X - c1X;
            double dy = c2Y - c1Y;
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

                double impulseX = nx * dotProduct;
                double impulseY = ny * dotProduct;

                // Nadpisujemy prędkości po zderzeniu z użyciem adaptera
                b1.Velocity = new DataVector(b1.Velocity.X + impulseX, b1.Velocity.Y + impulseY);
                b2.Velocity = new DataVector(b2.Velocity.X - impulseX, b2.Velocity.Y - impulseY);
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