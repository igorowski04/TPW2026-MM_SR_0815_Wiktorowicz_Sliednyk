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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TP.ConcurentPrograming.BusinessLogic;
using DataBall = TP.ConcurrentProgramming.Data.IBall;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
    internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
    {
        private bool Disposed = false;
        private readonly UnderneathLayerAPI layerBellow;
        private readonly IDiagnosticLogger _logger;

        // ===============
        // -=- ZMIENNE -=-
        // ===============
        private Timer? MoveTimer;
        private List<DataBall> _dataBalls = new(); // Lista kul pobranych z bazy danych
        private DataBall? _playerBall;
        private double _boardWidth;
        private double _boardHeight;
        // Kontroler do zatrzymywania wielu wątków na raz
        private CancellationTokenSource? _cancaleTokenSource;
        // To jest sekcja krytyczna, któa zabezpiecza przed wyścigiem (Race Condition)
        private readonly object _collisionLock = new object();


        #region ctor
        public BusinessLogicImplementation() : this(null, null) { }

        internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer = null, IDiagnosticLogger? logger = null)
        {
            layerBellow = underneathLayer ?? UnderneathLayerAPI.GetDataLayer();
            _logger = logger ?? new DiagnosticLogger();
        }
        #endregion ctor

        #region BusinessLogicAbstractAPI

        public override void Dispose()
        {
            if (Disposed) return;
            // W tym miejscu zatrzymywane są wszystkie asynchroniczne zadania
            _cancaleTokenSource?.Cancel();
            _cancaleTokenSource?.Dispose();

            _logger?.Dispose();
            layerBellow.Dispose();
            Disposed = true;
        }

        public override void Start(int numberOfBalls, double width, double height, Action<IPosition, IBall> upperLayerHandler)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BusinessLogicImplementation));

            // Zatrzymanie asynchronicznych zadań starych kulek
            _cancaleTokenSource?.Cancel();
            _cancaleTokenSource?.Dispose();

            _boardWidth = width;
            _boardHeight = height;
            _dataBalls.Clear();
            _playerBall = null;

            // Odpytujemy warstwę danych (bazę) o wygenerowanie kul
            layerBellow.Start(numberOfBalls, width, height, (startingPosition, dataBall) =>
            {
                _dataBalls.Add(dataBall);

                // Pierwsza wygenerowana Kula(ta o najniższym UUID) będzie sterowana przez myszkę
                if (_dataBalls.Count == 1)
                {
                    _playerBall = dataBall;
                }

                // Przekazujemy nową biznesową kulę wyżej do UI
                upperLayerHandler(new Position(startingPosition.X, startingPosition.Y), new BusinessBall(dataBall));
            });
            
            // W tym miejscu uruchomiona zostaje współbieżność.
            // Task.Run() dla każdej kuli tworzy osobne asynchroniczne Zadanie.
            // Zadania te nie tworzą wątków, lecz trafiaj do Puli Wątków (ThreadPool)
            // Systemowa Pula Wątków w tle przydziela im wolne wątki z procesora
            _cancaleTokenSource = new CancellationTokenSource();
            foreach (var ball in _dataBalls)
            {
                Task.Run(() => MoveBallAsync(ball, _cancaleTokenSource.Token));
            }
        }

        public override void UpdatePlayerPosition(double x, double y)
        {
            if (_playerBall != null)
            {
                lock (_playerBall)
                {
                    double safeX = Math.Max(0, Math.Min(x - _playerBall.Radius, _boardWidth - _playerBall.Radius * 2));
                    double safeY = Math.Max(0, Math.Min(y - _playerBall.Radius, _boardHeight - _playerBall.Radius * 2));
                    _playerBall.Position = new DataVector(safeX, safeY);
                }
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
                // 
                // 1. Wątek z Puli Wątków pobiera Zadanie przypisane do kuli 
                // 2. Wchodzi do tej funkcji i jak natrafia na await - kula zasypia na 20 ms
                //  a Wątek zostaje zwolniony spowrotem do puli 
                // 3. Zwolniony wątek natychmiast bierze z puli Zadanie kolejnej kuli, dochodzi do await
                //  usypia ją i bierze kolejną do momentu kiedy nie weźmie wszystkich
                //  (Cała operacja sprowadzania wszystkich kul trwa ułamek milisekundy)
                // 4. Po upływie 20 ms Kuli nr 1 spowrotem zostaje przydzielony wolny Wątek
                // 5. Wątek ten wchodzi do sekcji krytycznej oraz w ułamku milisekundy liczy nowe pozycje,
                //  odbicia od ścian i aktualizuje wektory. 
                // 6. Kula wychodzi z sekcji krytycznej oraz zostaje sprawdzona kolizja z innymi kulami za pomocą checkCollisions.
                // 7. Pętla wraca na początek, kula 1 trafia do await i wątek zostaje zwolniony. 
                //  Te wszystkie operajcje również trwają ułamek milisekundy 
                await Task.Delay(20, token);

                lock (ball)
                {   
                    // Kulka sterowana przez nas za pomocą myszki nie jest blokowana. 
                    // Nie jest sterowana automatycznie
                    if (ball != _playerBall)
                    {
                        double effectiveWidth = _boardWidth - (ball.Radius * 2);
                        double effectiveHeight = _boardHeight - (ball.Radius * 2);

                        double rawX = ball.Position.X + ball.Velocity.X;
                        double rawY = ball.Position.Y + ball.Velocity.Y;

                        var (newX, newVx) = CalculateModuloBounce(rawX, ball.Velocity.X, effectiveWidth);
                        var (newY, newVy) = CalculateModuloBounce(rawY, ball.Velocity.Y, effectiveHeight);

                        // Tutaj rejestrujemy zderzenie ze ścianą
                        if (newVx != ball.Velocity.X || newVy != ball.Velocity.Y)
                        {
                            _logger.LogCollision(ball, "Wall");
                        }

                        ball.Position = new DataVector(newX, newY);
                        ball.Velocity = new DataVector(newVx, newVy);
                    }
                    
                } 

                // 2. Kolizje sprawdzamy poza sekcją krytyczną ruchu. 
                // Inne wątki mogą teraz wchodzić do powyższego bloku dla innych kul.
                foreach (var otherball in _dataBalls)
                {
                    if (otherball == ball) continue;


                    if (CheckCollision(ball, otherball))
                    {
                        _logger.LogCollision(ball, $"Ball_{otherball.Id}");
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
        // Kolizje sprawdzane są w momencie wyjścia kuli z sekcji krytycznej 
        //  ale nadal z przypisanym tym samym wątkiem. 
        //  W sensie jak do kuli (Zadania) zostanie przypisany wątek: wejście do sekcji krytycznej -> wyjśie -> sprawdzenie kolizji
        private bool CheckCollision(DataBall b1, DataBall b2)
        {
            // Wstępne sprawdzenie dystansu (bez zakładania sekcji krytycznej jeszcze)
            double dx = b2.Position.X - b1.Position.X;
            double dy = b2.Position.Y - b1.Position.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Sprawdzamy, czy odległosci środków kul są równe sumie promieni kul
            if (distance == 0 || distance > b1.Radius + b2.Radius) return false;

            // Jeśli kule się zderzają, musimy zablokować obie na czas zmiany ich prędkości.
            // Używamy GetHashCode(), aby ZAWSZE blokować kule w tej samej kolejności,
            // co całkowicie eliminuje ryzyko zakleszczenia (Deadlocka).
            object firstLock = b1.GetHashCode() < b2.GetHashCode() ? b1 : b2;
            object secondLock = b1.GetHashCode() < b2.GetHashCode() ? b2 : b1;

            lock (firstLock)
            {
                lock (secondLock)
                {
                    // Ponownie sprawdzamy dystans, bo kule mogły się przesunąć
                    // w czasie, gdy czekaliśmy na założenie blokad
                    dx = b2.Position.X - b1.Position.X;
                    dy = b2.Position.Y - b1.Position.Y;
                    distance = Math.Sqrt(dx * dx + dy * dy);

                    // Jeśli po uzyskaniu blokad nadal są w kolizji - liczymy fizykę
                    if (distance <= b1.Radius + b2.Radius)
                    {
                        double nx = dx / distance;
                        double ny = dy / distance;

                        double relativeVelocityX = b2.Velocity.X - b1.Velocity.X;
                        double relativeVelocityY = b2.Velocity.Y - b1.Velocity.Y;

                        double dotProduct = relativeVelocityX * nx + relativeVelocityY * ny;

                        if (dotProduct > 0) return false;

                        double impulse = (2.0 * dotProduct) / (b1.Mass + b2.Mass);

                        double newVx1 = b1.Velocity.X + (impulse * b2.Mass * nx);
                        double newVy1 = b1.Velocity.Y + (impulse * b2.Mass * ny);

                        double newVx2 = b2.Velocity.X - (impulse * b1.Mass * nx);
                        double newVy2 = b2.Velocity.Y - (impulse * b1.Mass * ny);

                        // Aktualizacja wektorów
                        b1.Velocity = new DataVector(newVx1, newVy1);
                        b2.Velocity = new DataVector(newVx2, newVy2);

                        return true;
                    }
                }
            }
            return false;
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