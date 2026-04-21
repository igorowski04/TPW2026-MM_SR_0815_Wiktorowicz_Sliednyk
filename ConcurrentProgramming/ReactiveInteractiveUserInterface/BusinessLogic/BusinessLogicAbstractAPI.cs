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
            public static readonly Dimensions GetDimensions = new(10.0, 10.0, 10.0);
            public abstract void Start(int numberOfBalls, double width, double height, Action<IPosition, IBall> upperLayerHandler);
            
            #region IDisposable
                public abstract void Dispose();
            #endregion IDisposable

        #endregion Layer API
    }
    /// <summary>
    /// Immutable type representing table dimensions
    /// </remarks>
    public record Dimensions(double BallDimension, double TableHeight, double TableWidth);

    public interface IPosition
    {
        double x { get; init; }
        double y { get; init; }
    }

    public interface IBall 
    {
        event EventHandler<IPosition> NewPositionNotification;
    }
}