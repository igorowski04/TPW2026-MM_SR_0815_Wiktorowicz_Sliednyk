//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//_____________________________________________________________________________________________________________________________________

using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using UnderneathLayerAPI = TP.ConcurrentProgramming.BusinessLogic.BusinessLogicAbstractAPI;

namespace TP.ConcurrentProgramming.Presentation.Model
{

    internal class ModelImplementation : ModelAbstractApi
    {
        internal ModelImplementation() : this(null)
        { }

        internal ModelImplementation(UnderneathLayerAPI? underneathLayer)
        {
            layerBellow = underneathLayer ?? UnderneathLayerAPI.GetBusinessLogicLayer();
            eventObservable = Observable.FromEventPattern<BallChangedEventArgs>(this, "BallChanged");
        }

        #region ModelAbstractApi
            public override void Dispose()
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(ModelImplementation));
                layerBellow.Dispose();
                Disposed = true;
            }

            public override IDisposable Subscribe(IObserver<IBall> observer)
            {
                return eventObservable.Subscribe(x => observer.OnNext(x.EventArgs.Ball), ex => observer.OnError(ex), () => observer.OnCompleted());
            }

            public override void Start(int numberOfBalls, double width, double height)
            {
                layerBellow.Start(numberOfBalls, width, height, StartHandler);
            }
        #endregion ModelAbstractApi

        #region API
            public event EventHandler<BallChangedEventArgs>? BallChanged;
        #endregion API

        #region private
            private bool Disposed = false;
            private readonly IObservable<EventPattern<BallChangedEventArgs>> eventObservable;
            private readonly UnderneathLayerAPI layerBellow;

            private void StartHandler(BusinessLogic.IPosition position, BusinessLogic.IBall ball)
            {
                // Używamy dużych X i Y. Usunięto narzucone Diameter = 30.0!
                ModelBall newBall = new ModelBall(position.X, position.Y, ball);
                BallChanged?.Invoke(this, new BallChangedEventArgs() { Ball = newBall });
            }
        #endregion private

        #region TestingInfrastructure
            [Conditional("DEBUG")]
            internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
            {
                returnInstanceDisposed(Disposed);
            }

            [Conditional("DEBUG")]
            internal void CheckUnderneathLayerAPI(Action<UnderneathLayerAPI> returnNumberOfBalls)
            {
                returnNumberOfBalls(layerBellow);
            }

            [Conditional("DEBUG")]
            internal void CheckBallChangedEvent(Action<bool> returnBallChangedIsNull)
            {
                returnBallChangedIsNull(BallChanged == null);
            }
        #endregion TestingInfrastructure
    }

    // Poprawiona literówka (Chane -> Changed)
    public class BallChangedEventArgs : EventArgs
    {
        public required IBall Ball { get; init; }
    }
}