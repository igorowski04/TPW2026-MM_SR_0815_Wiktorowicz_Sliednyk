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

namespace TP.ConcurrentProgramming.Data
{
    public abstract class DataAbstractAPI : IDisposable
    {
        #region Layer Factory
            // Notka: zaimplementowane w taki sposób, żeby przy każdym wywołaniu tworzona jest nowa instancja warstwy danych.
            //        Zrobione tak, żeby podczas uruchomienia symulacji i zmiany stanu, nie wpływało to na inną instancję. 
            //        Dzięki temu testy są niezależne i zawsze sprawdzają żądaną funkcjonalność.
            public static DataAbstractAPI GetDataLayer()
            {
                return new DataImplementation();
            }
        #endregion Layer Factory

        #region public API
            // Nowe: zmienne height oraz width
            public abstract void Start(int numberOfBalls, double width, double height, Action<IVector, IBall> upperLayerHandler);
        #endregion public API

        #region IDisposable
            public abstract void Dispose();
        #endregion IDisposable

    }
    public interface IVector
    {
        double X { get; }
        double Y { get; }
    }

    
    public interface IBall
    {
        // Wyrzucilismy z warstwy danych całą fizykę (była tam metoda Move) teraz warstwa wyżej (logiki)
        // będzie pobierać z tej warstwy tylko prędkość kuli, promień, a pozycję będzie nadpisywać
        event EventHandler<IVector> NewPositionNotification;
        IVector Velocity { get; set; }
        IVector Position { get; set; }
        double Radius { get; }
    }
}