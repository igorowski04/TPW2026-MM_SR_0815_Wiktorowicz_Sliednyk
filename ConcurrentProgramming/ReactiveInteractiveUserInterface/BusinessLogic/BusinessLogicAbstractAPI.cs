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

namespace TP.ConcurrentProgramming.BusinessLogic
{
    public abstract class BusinessLogicAbstractAPI : IDisposable
    {
        #region Layer Factory
            public static BusinessLogicAbstractAPI GetBusinessLogicLayer()
            {
                // Tak jak w przypadku warstwy danych: tworzymy zawsze nową instancję, aby każdy test był niezależny
                return new BusinessLogicImplementation();
            }
        #endregion Layer Factory

        #region Layer API
            public abstract void Start(int numberOfBalls, double width, double height, Action<IPosition, IBall> upperLayerHandler);
            
            #region IDisposable
                public abstract void Dispose();
        #endregion IDisposable

        #endregion Layer API

        // TUTAJ FUNKCJA DO PRZESUWANIA MYSZY
        public abstract void UpdatePlayerPosition(double x, double y);
    }
    // Wycieliśmy Position, ponieważ teraz okno będzie przystosowywać się do naszego okna dialogowego
    // Wcześniej na szywno ustawialiśmy wielkość kuli i okna. 
    public interface IPosition
    {   
        // Usunięte init, ponieważ jest to interfejs kontrakt pomiędzy warstwą logiki,
        //  a interfejsem graficznym. Nie potrzebuje on ustawiać tej wartości na nowo, 
        //  tylko ją odczytać
        double X { get; }
        double Y { get; }
    }

    public interface IBall 
    {
        event EventHandler<IPosition> NewPositionNotification;

        double Radius { get; }
    }
}