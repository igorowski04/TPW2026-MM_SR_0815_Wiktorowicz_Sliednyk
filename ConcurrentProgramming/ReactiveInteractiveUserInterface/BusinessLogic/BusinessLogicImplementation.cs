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
            // Task.Run() dla każdej kuli pobiera wolny wątek z tzw. Puli Wątków (ThreadPod)
            // i zleca wykonywanie wykonywane metody MoveBallAsync
            _cancaleTokenSource = new CancellationTokenSource();
            foreach (var ball in _dataBalls)
            {
                Task.Run(() => MoveBallAsync(ball, _cancaleTokenSource.Token));
            }
        }

        #endregion BusinessLogicAbstractAPI

        #region Physics Engine (Private)

        // Pętla asynchroniczna życia kuli
        private async Task MoveBallAsync(DataBall ball, CancellationToken token)
        {
            
            /// Dopuki nie zamkniemy okna, albo nie wywołamy Dispose() - pętla while się kręci
            while (!token.IsCancellationRequested)
            {   
                // Tutaj nie usypiamy kuli, tylko zwalniamy wątek do puli wątków. 
                // Aby nie tworzyć 100 wątków do 100 kul, po wejściu do metody przebgieg jest następujący: 
                // 1. do kuli przypisany jest wątek. 2. wchodzi w tym momencie w sekcję krytyczną 
                // 3. liczy nową pozycję odbicia, no wszystko co niżej 4. Wątek jest zwalniany i odsyłany do puli 
                // 5. Ta kula musi czekać teraz 20 ms, aż inny wątek zostanie do niej przypisany i zmieni jej pozycję
                // 20 ms = 50 FPS. Czemu? 
                // 1 sekunda = 1000 ms. 1000 / 20 = 50.
                await Task.Delay(20, token); 

                // SEKCJA KRYTYCZNA: W danym momencie tylko jedna kula może aktualizować wektory i badać kolizje
                lock (_collisionLock)
                {
                    // Tu odjęta została została średnica kuli, żeby łatwiej poruszać się po prostokącie
                    double effectiveWidth = _boardWidth - (ball.Radius * 2);
                    double effectiveHeight = _boardHeight - (ball.Radius * 2);

                    // Pozycja kuli
                    double rawX = ball.Position.X + ball.Velocity.X;
                    double rawY = ball.Position.Y + ball.Velocity.Y;

                    var (newX, newVx) = CalculateModuloBounce(rawX, ball.Velocity.X, effectiveWidth);
                    var (newY, newVy) = CalculateModuloBounce(rawY, ball.Velocity.Y, effectiveHeight);

                    ball.Position = new DataVector(newX, newY);
                    ball.Velocity = new DataVector(newVx, newVy);

                    // Kolizje miedzy kulami 
                    foreach (var otherball in _dataBalls)
                    {
                        if (otherball == ball) continue;
                        CheckCollision(ball, otherball);
                    }
                }
            }
        }

        // |==========================|
        // |-=- ODBICIA KUL MODULO -=-|
        // |==========================|
        // W tej metodzie opracowany został algorytm, który obsługuje prędkości większe niż rozmiar planszy
        // Algorytm na początku liczy, ile pełnych okresów (z A do B i z B do A) mógłby zrobić, żeby pozostać w tej samej pozycji
        // Przykładowo, jeśli po wyliczona pozycja w następnej chwili, czyli pozycja + prędkośść to 1050, przy szerokości 100
        // oblicza, że w 1050 pełen okres (czyli 2x szer. planszy[albo wys.]) mieści się 5 razy. wyciąga resztę z dzielenia, 
        // następnie oblicza standardowe odbicie dla wartości mieszczącej się w zakresie. Finito
        private (double newPos, double newVel) CalculateModuloBounce(double rawPos, double vel, double maxPos)
        {
            if (maxPos <= 0) return (0, vel);

            double M = ((rawPos % (2 * maxPos)) + (2 * maxPos)) % (2 * maxPos);

            double finalPos = maxPos - Math.Abs(M - maxPos);

            int directionMultipier = (M <= maxPos) ? 1 : -1;

            double finalVel = vel * directionMultipier;

            return (finalPos, finalVel);
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