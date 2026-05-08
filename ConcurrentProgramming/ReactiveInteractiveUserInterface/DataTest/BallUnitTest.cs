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

namespace TP.ConcurrentProgramming.Data.Test
{
    [TestClass]
    public class BallUnitTest
    {   
        
        [TestMethod]
        /// |==============================|
        /// |-=- POPRAWNA INICJALIZACJA -=-|
        /// |==============================|
        public void ConstructorTestMethod()
        {
            Vector testingVector = new Vector(0.0, 0.0);
            // Podajemy wektor początkowy, wektor prędkości i promień
            Ball newInstance = new(testingVector, testingVector, 15.0);
            
            Assert.IsNotNull(newInstance);
            Assert.AreEqual(15.0, newInstance.Radius);
            Assert.AreEqual(testingVector, newInstance.Position);
            Assert.AreEqual(testingVector, newInstance.Velocity);
        }


        [TestMethod]
        // |==============================================|
        // |-=- POWIADAMIANIE O ZMIANIE POZYCJI DZIAŁA -=-|
        // |==============================================|
        public void PositionChangeNotificationTestMethod()
        {
            Vector startPosition = new Vector(0.0, 0.0);
            Ball ball = new Ball(startPosition, startPosition, 15.0);

            Vector newPosition = new Vector(10.0, 20.0);
            bool eventWasInvoked = false;

            ball.NewPositionNotification += (sender, vector) =>
            {
                eventWasInvoked = true;

                Assert.AreEqual(10.0, vector.X);
                Assert.AreEqual(20.0, vector.Y);
            };

            ball.Position = newPosition;

            // Jeśli Event nasłuchiwania zmienił się na true - jest true, Jeśli nie - Komunikat wyświetli
            Assert.IsTrue(eventWasInvoked, "Zdarzenie NewPositionNotification nie zostało wywołane podczas zmiany pozycji");
        }
    }

}