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

namespace TP.ConcurrentProgramming.BusinessLogic.Test
{
    [TestClass]
    public class BusinessBallUnitTest
    {
        [TestMethod]
        // |=================================================================|
        // |-=- BUSINESS BALL POPRAWNIE TŁUMACZY EVENTY Z NIŻSZEJ WARSTWY -=-|
        // |=================================================================|
        public void EventTranslationTestMethod()
        {
            DataBallFixture dataBallFixture = new DataBallFixture();
            BusinessBall newInstance = new(dataBallFixture);
            int numberOfCallBackCalled = 0;

            newInstance.NewPositionNotification += (sender, position) =>
            {
                Assert.IsNotNull(sender);
                Assert.IsNotNull(position);
                Assert.AreEqual(10.0, position.X);
                numberOfCallBackCalled++;
            };

            dataBallFixture.TriggerMove();

            Assert.AreEqual<int>(1, numberOfCallBackCalled);
        }

        // W tym pliku testowym sprawdzamy, czy poprawnie odbieramy sygnały z warstwy danych.
        // Nie możemy stworzyć instancji kuli z warstwy danych, bo jeśli coś się zepsuje - nie będziemy wiedzieć, która warstwa zawodzi
        // W tym celu tworzymy sztuczne klasy wydmuszki z warstwy danych, których użyliśmy wyżej w testach
        #region testingInstrumentation
        private class DataBallFixture : Data.IBall
        {
            public Data.IVector Velocity { get; set; } = new VectorFixture(0, 0);
            public Data.IVector Position { get; set; } = new VectorFixture(0, 0);
            public double Radius { get; } = 15.0; // Dodano brakujący promień!

            public event EventHandler<Data.IVector>? NewPositionNotification;

            internal void TriggerMove()
            {
                // Sztucznie wywołujemy ruch na pozycję 10, 20
                NewPositionNotification?.Invoke(this, new VectorFixture(10.0, 20.0));
            }
        }

        private class VectorFixture : Data.IVector
        {
            internal VectorFixture(double x, double y)
            {
                X = x; Y = y;
            }

            public double X { get; }
            public double Y { get; }
        }
        #endregion testingInstrumentation

    }
}