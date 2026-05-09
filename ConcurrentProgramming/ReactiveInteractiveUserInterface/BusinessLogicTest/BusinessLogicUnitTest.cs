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
        /// <summary>
        /// |============================================|
        /// |-=- TEST STARTU (TWORZENIE KUL)          -=-|
        /// |============================================|
        /// Sprawdza, czy metoda Start poprawnie komunikuje się z warstwą danych,
        /// zleca wygenerowanie odpowiedniej liczby kul i uruchamia callback do UI.
        /// </summary>
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
        /// <summary>
        /// |============================================|
        /// |-=- BARDZO WAŻNY TEST KOLIZJI (3 KULE)   -=-|
        /// |============================================|
        /// Włamuje się przez mechanizm Refleksji do prywatnej metody silnika fizycznego (CheckCollision)
        /// i upewnia się, że wektory prędkości faktycznie zmieniają się w wyniku zderzenia.
        /// </summary>
        public void ThreeBallsCollisionTestMethod()
        {
            // 1. Arrange: Przygotowujemy sztuczne wektory i kule testowe (zaślepki)
            VectorFixture pos1 = new VectorFixture(0, 0);
            VectorFixture pos2 = new VectorFixture(28, 0);
            VectorFixture pos3 = new VectorFixture(14, 24);

            VectorFixture vel1 = new VectorFixture(5, 5);
            VectorFixture vel2 = new VectorFixture(-5, 5);
            VectorFixture vel3 = new VectorFixture(0, -5);

            BallFixture ball1 = new BallFixture() { Position = pos1, Velocity = vel1 };
            BallFixture ball2 = new BallFixture() { Position = pos2, Velocity = vel2 };
            BallFixture ball3 = new BallFixture() { Position = pos3, Velocity = vel3 };

            // Tworzymy logikę biznesową z "pustą" bazą danych
            BusinessLogicImplementation businessLogic = new(new DataLayerStartFixcure());

            // Włamujemy się do prywatnej metody CheckCollision za pomocą Refleksji
            var checkCollisionMethod = typeof(BusinessLogicImplementation).GetMethod("CheckCollision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 2. Act: Symulujemy zderzenia między wszystkimi kulami po kolei
            checkCollisionMethod!.Invoke(businessLogic, new object[] { ball1, ball2 });
            checkCollisionMethod.Invoke(businessLogic, new object[] { ball1, ball3 });
            checkCollisionMethod.Invoke(businessLogic, new object[] { ball2, ball3 });

            // 3. Assert: Sprawdzamy czy wektory się zmieniły po odbiciach
            Assert.AreNotEqual<double>(5.0, ball1.Velocity.X);
            Assert.AreNotEqual<double>(5.0, ball1.Velocity.Y);

            Assert.AreNotEqual<double>(-5.0, ball2.Velocity.X);
            Assert.AreNotEqual<double>(5.0, ball2.Velocity.Y);

            Assert.AreNotEqual<double>(0.0, ball3.Velocity.X);
            Assert.AreNotEqual<double>(-5.0, ball3.Velocity.Y);
        }

        #region testing instrumentation

        // Wyciągnięte na zewnątrz zaślepki ("puste" makiety) ułatwiające dostęp dla wszystkich testów

        internal record VectorFixture(double X, double Y) : Data.IVector;

        internal class BallFixture : Data.IBall
        {
            public Data.IVector Velocity { get; set; } = new VectorFixture(0, 0);
            public Data.IVector Position { get; set; } = new VectorFixture(0, 0);
            public double Radius { get; } = 15.0;
            public double Mass { get; } = 1.0;

            public event EventHandler<Data.IVector>? NewPositionNotification;
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