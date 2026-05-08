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
using System.Collections.Generic;

namespace TP.ConcurrentProgramming.Data.Test
{
    [TestClass]
    public class DataImplementationUnitTest
    {
        [TestMethod]
        /// |==============================================|
        /// |-=- KONSTRUKTOR DATA IMPLEMENTATION DZIALA -=-|
        /// |==============================================|
        public void ConstructorTestMethod()
        {
            using (DataImplementation newInstance = new DataImplementation())
            {
                IEnumerable<IBall>? ballsList = null;
                newInstance.CheckBallsList(x => ballsList = x);
                Assert.IsNotNull(ballsList);
                int numberOfBalls = 0;
                newInstance.CheckNumberOfBalls(x => numberOfBalls = x);
                Assert.AreEqual<int>(0, numberOfBalls);
            }
        }

        [TestMethod]
        /// |=========================================================|
        /// |-=- NIE USUNIESZ USUNIĘTEGO / NIEISTNIEJĄCEGO OBIEKTU -=-|
        /// |=========================================================|
        public void DisposeTestMethod()
        {
            DataImplementation newInstance = new DataImplementation();
            bool newInstanceDisposed = false;
            newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
            Assert.IsFalse(newInstanceDisposed);

            newInstance.Dispose();

            newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
            Assert.IsTrue(newInstanceDisposed);
            IEnumerable<IBall>? ballsList = null;
            newInstance.CheckBallsList(x => ballsList = x);
            Assert.IsNotNull(ballsList);
            newInstance.CheckNumberOfBalls(x => Assert.AreEqual<int>(0, x));

            // Dodano testowe wymiary do metody Start
            Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Start(10, 700, 400, (position, ball) => { }));
        }

        [TestMethod]
        /// |===========================|
        /// |-=- METODA START DZIAŁA -=-|
        /// |===========================|
        /// | - Test sprawdza, czy metoda Start generuje kule i komunikuje sięz warstwami wyżej
        public void StartTestMethod()
        {
            using (DataImplementation newInstance = new DataImplementation())
            {
                int numberOfCallbackInvoked = 0;
                int numberOfBalls2Create = 10;

                // Dodano testowe wymiary ekranu
                newInstance.Start(
                  numberOfBalls2Create,
                  700.0,
                  400.0,
                  (startingPosition, ball) =>
                  {
                      numberOfCallbackInvoked++;
                      Assert.IsTrue(startingPosition.X >= 0);
                      Assert.IsTrue(startingPosition.Y >= 0);
                      Assert.IsNotNull(ball);
                  });

                Assert.AreEqual<int>(numberOfBalls2Create, numberOfCallbackInvoked);
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual<int>(10, x));
            }
        }
        
    }
}