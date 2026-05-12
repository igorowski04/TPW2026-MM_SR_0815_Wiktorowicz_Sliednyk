//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.CompilerServices;
using TP.ConcurrentProgramming.Data;

namespace TP.ConcurrentProgramming.BusinessLogic.Test
{
    [TestClass]
    public class BusinessLogicImplementationUnitTest
    {
        [TestMethod]
        /// |============================================|
        /// |-=- TEST KONSTRUKTORA (INICJALIZACJA)    -=-|
        /// |============================================|
        /// Sprawdza, czy obiekt BusinessLogicImplementation tworzy się poprawnie
        /// i nie jest od razu oznaczony jako usunięty (Disposed = false).
        public void ConstructorTestMethod()
        {
            using (BusinessLogicImplementation newInstance = new(new DataLayerConstructorFixcure()))
            {
                bool newInstanceDisposed = true;
                newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
                Assert.IsFalse(newInstanceDisposed);
            }
        }

        [TestMethod]
        /// |============================================|
        /// |-=- TEST ZWALNIANIA ZASOBÓW (DISPOSE)    -=-|
        /// |============================================|
        /// Sprawdza, czy metoda Dispose poprawnie zwalnia zasoby (w tym warstwę niższa)
        /// oraz czy wbudowano zabezpieczenia przed próbą użycia już usuniętego obiektu.
        /// </summary>
        public void DisposeTestMethod()
        {
            DataLayerDisposeFixcure dataLayerFixcure = new DataLayerDisposeFixcure();
            BusinessLogicImplementation newInstance = new(dataLayerFixcure);
            Assert.IsFalse(dataLayerFixcure.Disposed);

            bool newInstanceDisposed = true;
            newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
            Assert.IsFalse(newInstanceDisposed);

            newInstance.Dispose();

            newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
            Assert.IsTrue(newInstanceDisposed);

            // Oczekujemy, że program wyrzuci błąd, gdy spróbujemy użyć usuniętego obiektu
            Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Start(0, 100.0, 100.0, (position, ball) => { }));
            Assert.IsTrue(dataLayerFixcure.Disposed);
        }

        [TestMethod]
        /// |===================================|
        /// |-=- TEST STARTU (TWORZENIE KUL) -=-|
        /// |===================================|
        /// Sprawdza, czy metoda Start poprawnie komunikuje się z warstwą danych,
        /// zleca wygenerowanie odpowiedniej liczby kul i uruchamia callback do UI.
        public void StartTestMethod()
        {
            DataLayerStartFixcure dataLayerFixcure = new();
            using (BusinessLogicImplementation newInstance = new(dataLayerFixcure))
            {
                int called = 0;
                int numberOfBalls2Create = 10;

                newInstance.Start(
                  numberOfBalls2Create,
                  100.0,
                  100.0,
                  (startingPosition, ball) => { called++; Assert.IsNotNull(startingPosition); Assert.IsNotNull(ball); });

                Assert.AreEqual<int>(1, called);
                Assert.IsTrue(dataLayerFixcure.StartCalled);
                Assert.AreEqual<int>(numberOfBalls2Create, dataLayerFixcure.NumberOfBallseCreated);
            }
        }

        [TestMethod]
        // |=====================================|
        // |-=- TEST MA CZAS WĄTKI 10 SEKUND! -=-|
        // |=====================================|
        // Test sprawdza, czy procesor sprawiedliwie przydzielił czas każdej kuli
        // Weryfikuje, czy przez 10s. działania programu każda z 5 kul miała tyle samo okazji
        // do przeliczenia swojej pozycji, tak, aby żadna kula nie zdominowała procesora
        // czyli nie doszło to tzw. zjawiska zagłodzenia (Thread Starvation)
        public async Task SchedulingFairnessTestAsync()
        {
            int numberOfBalls = 5;
            var mockDataLayer = new FairnessTestDataLayer();
            using BusinessLogicImplementation businessLogic = new(mockDataLayer);

            Dictionary<object, int> updateCounts = new();

            businessLogic.Start(numberOfBalls, 500.0, 500.0, (position, ball) =>
            {
                // Słownik, który dla każdej kuli liczy ile razy wywołała ona zdarzenie o zmianie pozycji
                updateCounts[ball] = 0;

                ball.NewPositionNotification += (sender, args) =>
                {
                    lock (updateCounts)
                    {
                        updateCounts[ball]++;
                    }
                };
            });

            // W tym miejscu uruchamiamy nasłuch. Trwa on 10000 ms, czyli 10 s.
            // Nie ogranicza to ruchu kul; w sensie nie muszą one czekać 10 sekund na ruch (bo one elegancko
            // pracują sobie w tle ze zmianą co 20 ms), tylko opóźnienie w usnięciu wartstwy logiki.
            // W tym czasie dla każdej kuli zliczana jest liczba wystąpień
            await Task.Delay(10000);
            businessLogic.Dispose();
            Assert.AreEqual(numberOfBalls, updateCounts.Count, "Powinno wygenerować się dokładnie 5 kul");


            int minUpdates = int.MaxValue;
            int maxUpdates = int.MinValue;

            // Tu dla każdej wartości z tej tabliczy ze zliczeniami ustalamy największą i najmniejszą wartość.
            // Każdy wątek powinien pracować tyle samo +/- 1 razy, i jeśli mamy 5 kul = 5 wątków -
            // błąd powinien wynosić max 5
            foreach (var count in updateCounts.Values)
            {
                if (count < minUpdates) minUpdates = count;
                if (count > maxUpdates) maxUpdates = count;

                int difference = maxUpdates - minUpdates;
                
                Assert.IsTrue(difference <= 5, $"Sprawiedliwość wątków zachwiana. Max odświeżeń: {maxUpdates}, Min: {minUpdates}, różnica: {difference}");
            }

        }

        [TestMethod]
        /// |==================================================|
        /// |-=- TEST ZATRZYMANIA WĄTKÓW (CANCELLATION)     -=-|
        /// |==================================================|
        /// Sprawdza, czy wykonanie metody Dispose (która wywołuje Cancel na Tokenie)
        /// ostatecznie i definitywnie ubija pętle asynchroniczne kul.
        public async Task CancellationAndThreadStopTestAsync()
        {
            int numberOfBalls = 3;
            var mockDataLayer = new FairnessTestDataLayer();
            BusinessLogicImplementation businessLogic = new(mockDataLayer);

            Dictionary<object, int> updateCounts = new();

            // 1. Uruchamiamy kule
            businessLogic.Start(numberOfBalls, 500.0, 500.0, (position, ball) =>
            {
                updateCounts[ball] = 0;

                ball.NewPositionNotification += (sender, args) =>
                {
                    lock (updateCounts)
                    {
                        updateCounts[ball]++;
                    }
                };
            });

            // Pozwalamy kulom generować pozycje przez 200 ms
            await Task.Delay(200);

            businessLogic.Dispose();

            // Zapisujemy liczbę wywołań zdarzeń od razu po wyłączeniu wątków
            Dictionary<object, int> countsAfterDispose = new(updateCounts);

            // Czekamy kolejne 200 ms, żeby upewnić się, że zatrzymanie poskutkowało
            await Task.Delay(200);

            // 3. Assert: Wartości sprzed 200 ms i obecne muszą być takie same
            foreach (var ball in updateCounts.Keys)
            {
                Assert.AreEqual(countsAfterDispose[ball], updateCounts[ball],
                    "Wątek kuli wysłał nową pozycję nawet po wywołaniu Dispose! Anulowanie Taska nie działa poprawnie.");
            }
        }

        [TestMethod]
        /// |=========================================|
        /// |-=- TEST KOLIZJI 3 KUL JEDNOCZEŚNIE   -=-|
        /// |=========================================|
        /// Włamuje się przez mechanizm Refleksji do prywatnej metody silnika fizycznego (CheckCollision)
        /// i upewnia się, że wektory prędkości faktycznie zmieniają się w wyniku zderzenia.
        public void ThreeBallsCollisionTestMethod()
        {
            // 1. Arrange: Idealne ułożenie łańcuchowe (każda kolizja wymusi zmianę na X oraz Y)
            // Promień kul = 15, więc zasięg kolizji to <= 30.
            VectorFixture pos1 = new VectorFixture(0, 0);
            VectorFixture pos2 = new VectorFixture(20, 0);     // Uderzy w Kula 1 (odległość = 25)
            VectorFixture pos3 = new VectorFixture(10, 17);     // Uderzy w Kula 2 po jej odbiciu (odległość = 14)

            VectorFixture vel1 = new VectorFixture(5, 5);
            VectorFixture vel2 = new VectorFixture(-5, 5);
            VectorFixture vel3 = new VectorFixture(0, -10);

            BallFixture ball1 = new BallFixture() { Position = pos1, Velocity = vel1 };
            BallFixture ball2 = new BallFixture() { Position = pos2, Velocity = vel2 };
            BallFixture ball3 = new BallFixture() { Position = pos3, Velocity = vel3 };

            // Tworzymy logikę biznesową z "pustą" bazą danych
            BusinessLogicImplementation businessLogic = new(new DataLayerStartFixcure());

            // Włamujemy się do prywatnej metody CheckCollision za pomocą Refleksji
            var checkCollisionMethod = typeof(BusinessLogicImplementation).GetMethod("CheckCollision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 2. Act: Symulujemy zderzenia między wszystkimi 3 kulami po kolei
            checkCollisionMethod!.Invoke(businessLogic, new object[] { ball1, ball2 });
            checkCollisionMethod.Invoke(businessLogic, new object[] { ball1, ball3 });
            checkCollisionMethod.Invoke(businessLogic, new object[] { ball2, ball3 });

            // 3. Assert: Zmienione pozycje gwarantują, że zderzenia były całkowicie asymetryczne
            // więc żadna oś X i Y w żadnej kuli nie zachowa swojej pierwotnej wartości
            Assert.AreNotEqual<double>(5.0, ball1.Velocity.X);
            Assert.AreNotEqual<double>(5.0, ball1.Velocity.Y);

            Assert.AreNotEqual<double>(-5.0, ball2.Velocity.X);
            Assert.AreNotEqual<double>(5.0, ball2.Velocity.Y);

            Assert.AreNotEqual<double>(0.0, ball3.Velocity.X);
            Assert.AreNotEqual<double>(-10.0, ball3.Velocity.Y);
        }

        [TestMethod]
        /// |====================================|
        /// |-=- TEST ODBICIA OD ROGU PLANSZY -=-|
        /// |====================================|
        /// Sprawdza przypadek, w którym kula uderza idealnie w róg planszy
        /// (przekracza maksymalne granice zarówno dla osi X, jak i Y).
        /// Algorytm powinien zawrócić wektory dla obu osi jednocześnie.
        public void CornerBOunceTestMethod()
        {
            using BusinessLogicImplementation businessLogic = new(new DataLayerStartFixcure());
            var moduloBounceMethod = typeof(BusinessLogicImplementation).GetMethod("CalculateModuloBounce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // { pozycja = 105/108, prędkość = 10.0, plansza = 100x100 }
            object[] parametersCornerX = new object[] { 105.0, 10.0, 100.0 };
            object[] parametersCornerY = new object[] { 108.0, 10.0, 100.0 };

            // 2. Act: Wykonujemy odbicie dla osi X oraz osi Y
            var resultX = (ValueTuple<double, double>)moduloBounceMethod!.Invoke(businessLogic, parametersCornerX);
            var resultY = (ValueTuple<double, double>)moduloBounceMethod.Invoke(businessLogic, parametersCornerY);

            // 3. Assert: 
            // - Pozycja X i Y musi zostać skorygowana o to, jak głęboko kula "wpadła" w ścianę.
            //   Dla X: 100 - (105 - 100) = 95.0
            //   Dla Y: 100 - (108 - 100) = 92.0
            Assert.AreEqual(95.0, resultX.Item1);
            Assert.AreEqual(92.0, resultY.Item1);

            // - Obie osie prędkości muszą zmienić znak z (+10) na (-10), co oznacza 
            //   idealne odwrócenie lotu kuli (z powrotem do środka planszy).
            Assert.AreEqual(-10.0, resultX.Item2);
            Assert.AreEqual(-10.0, resultY.Item2);
        }

        [TestMethod]
        /// |==================================|
        /// |-=- TEST ODBICIA KUL OD ŚCIANY -=-|
        /// |==================================|
        /// Sprawdza algorytm CalculateModuloBounce dla krawędzi planszy
        /// Upewnia sioę, że wektor prędkości zmienia znak, a pozycja nie ucieka za mapę.
        public void WallBounceTestMethod()
        {
            using BusinessLogicImplementation businessLogic = new(new DataLayerStartFixcure());
            var moduloBounceMethod = typeof(BusinessLogicImplementation).GetMethod("CalculateModuloBounce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // |-=- Scenariusz 1: kula uderza w prawą ścianę -=-|

            // { pozycja = 105.0, prędkość=5.0, zasięg planszt=100.0 }
            object[] parametersRightWall = new object[] { 105.0, 5.0, 100.0 };

            var resultRight = (ValueTuple<double, double>)moduloBounceMethod!.Invoke(businessLogic, parametersRightWall);

            // Kula ma odbić się na 95 czyli 5 jednostek od prawej krawędzi 
            // Prędkość zmieni znak na ujemny
            Assert.AreEqual(95.0, resultRight.Item1);
            Assert.AreEqual(-5.0, resultRight.Item2);

            // |-=- Scenariusz 2: kula uderza w lewą ścianę -=-|

            // { pozycja = -5.0, prędkość = -5.0, zasięg planszy = 100.0 }
            object[] parametersLeftWall = new object[] { -5.0, -5.0, 100.0 };
            var resultLeft = (ValueTuple<double, double>)moduloBounceMethod.Invoke(businessLogic, parametersLeftWall);

            // Kula ma odbić się na pozycję 5.0 czyli 5 jednostek od lewej strony
            // Prędkość zmieni znak na dodatni
            Assert.AreEqual(5.0, resultLeft.Item1);
            Assert.AreEqual(5.0, resultLeft.Item2);
        }

        [TestMethod]
        /// |====================================================|
        /// |-=- TEST ODDALAJĄCYCH SIĘ KUL (BRAK ZDERZENIA)   -=-|
        /// |====================================================|
        /// Symuluje przypadek, gdy dwie kule "nachodzą" na siebie (odległość < 2x Radius),
        /// ale ich wektory prędkości wskazują, że już się od siebie oddalają.
        /// Wektor prędkości powinien pozostać bez zmian.
        public void OverlappingSeparatingBallsCollisionTestMethod()
        {
            // pozycje kul (X, Y)
            VectorFixture pos1 = new VectorFixture(10, 10);
            VectorFixture pos2 = new VectorFixture(15, 15);

            // Prędkości kul (X, Y)
            VectorFixture vel1 = new VectorFixture(-5, -5); // Kula 1 leci w górę-lewo
            VectorFixture vel2 = new VectorFixture(5, 5);   // Kula 2 leci w dół-prawo

            // Kule, które mają nachodzącą na siebie pozycję, ale lecą w inne strony.
            BallFixture ball1 = new BallFixture() { Position = pos1, Velocity = vel1 };
            BallFixture ball2 = new BallFixture() { Position = pos2, Velocity = vel2 };

            using BusinessLogicImplementation businessLogic = new(new DataLayerStartFixcure());
            var checkCollisionMethod = typeof(BusinessLogicImplementation).GetMethod("CheckCollision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Metoda do sprawdzenia zderzenia
            checkCollisionMethod!.Invoke(businessLogic, new object[] { ball1, ball2 });

            // 3. Assert: Wektory nie powinny ulec żadnej zmianie, ponieważ DotProduct jest dodatni
            Assert.AreEqual(-5.0, ball1.Velocity.X);
            Assert.AreEqual(-5.0, ball1.Velocity.Y);

            Assert.AreEqual(5.0, ball2.Velocity.X);
            Assert.AreEqual(5.0, ball2.Velocity.Y);
        }

        #region testing instrumentation

        // Wyciągnięte na zewnątrz zaślepki ("puste" makiety) ułatwiające dostęp dla wszystkich testów

        internal record VectorFixture(double X, double Y) : Data.IVector;

        internal class BallFixture : Data.IBall
        {
            public Data.IVector Velocity { get; set; } = new VectorFixture(0, 0);
            private Data.IVector _position = new VectorFixture(0, 0);
            public Data.IVector Position
            {
                get => _position;
                set
                {
                    _position = value;
                    NewPositionNotification?.Invoke(this, _position);
                }
            }

            public double Radius { get; } = 15.0;
            public double Mass { get; } = 1.0;

            public event EventHandler<Data.IVector>? NewPositionNotification;
        }

        private class FairnessTestDataLayer : Data.DataAbstractAPI
        {
            public override void Dispose() { }

            public override void Start(int numerOfBalls, double width, double heigt, Action<Data.IVector, Data.IBall> upperLayerHandler)
            {
                for (int i = 0; i < numerOfBalls; i++)
                {
                    upperLayerHandler(new VectorFixture(0, 0), new BallFixture());
                }
            }
        }

        private class DataLayerConstructorFixcure : Data.DataAbstractAPI
        {
            public override void Dispose() { }

            public override void Start(int numberOfBalls, double width, double height, Action<Data.IVector, Data.IBall> upperLayerHandler)
            {
                throw new NotImplementedException();
            }
        }

        private class DataLayerDisposeFixcure : Data.DataAbstractAPI
        {
            internal bool Disposed = false;

            public override void Dispose()
            {
                Disposed = true;
            }

            public override void Start(int numberOfBalls, double width, double height, Action<Data.IVector, Data.IBall> upperLayerHandler)
            {
                throw new NotImplementedException();
            }
        }

        private class DataLayerStartFixcure : Data.DataAbstractAPI
        {
            internal bool StartCalled = false;
            internal int NumberOfBallseCreated = -1;

            public override void Dispose() { }

            public override void Start(int numberOfBalls, double width, double height, Action<Data.IVector, Data.IBall> upperLayerHandler)
            {
                StartCalled = true;
                NumberOfBallseCreated = numberOfBalls;

                // Odpalamy callback ze sztucznym wektorem i sztuczną kulą
                upperLayerHandler(new VectorFixture(0, 0), new BallFixture());
            }
        }

        #endregion testing instrumentation
    }
}