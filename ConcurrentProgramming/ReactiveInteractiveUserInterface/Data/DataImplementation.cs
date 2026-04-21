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
using System.Threading;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        #region ctor
            public DataImplementation()
            {
                // startuje dopiero w metodzie start
            }
        #endregion ctor

        #region DataAbstractAPI

        public override void Start(int numberOfBalls, double width, double height,Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            MoveTimer?.Dispose();
            BallsList.Clear();

            for (int i = 0; i < numberOfBalls; i++) {
                double startX = RandomGenerator.NextDouble() * (width - 2 * BallRadius);
                double startY = RandomGenerator.NextDouble() * (height - 2 * BallRadius);
                Vector startingPosition = new(startX, startY);

                double vx = (RandomGenerator.NextDouble() - 0.5) * 12;
                double vy = (RandomGenerator.NextDouble() - 0.5) * 12;
                Vector initialVelocity = new(vx, vy);

                Ball newBall = new(startingPosition, initialVelocity, BallRadius, width, height);
                BallsList.Add(newBall);
                upperLayerHandler(startingPosition, newBall);
            }

            MoveTimer = new Timer(Move, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        #endregion DataAbstractAPI

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    MoveTimer.Dispose();
                    BallsList.Clear();
                }
            Disposed = true;
            }
            else
                throw new ObjectDisposedException(nameof(DataImplementation));
        }

        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region private

        //private bool disposedValue;
        private bool Disposed = false;
        private Timer? MoveTimer;
        private Random RandomGenerator = new();
        private List<Ball> BallsList = [];

        private readonly double BallRadius = 15.0;

        private void Move(object? state)
        {
            
            try
            {
                MoveTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                foreach (Ball item in BallsList)
                {
                    item.Move((Vector)item.Velocity);
                }
            }
            finally
            {
                MoveTimer?.Change(20, Timeout.Infinite);
            }

        }
     

        #endregion private

        #region TestingInfrastructure

        [Conditional("DEBUG")]
        internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
        {
            returnBallsList(BallsList);
        }

        [Conditional("DEBUG")]
        internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
        {
            returnNumberOfBalls(BallsList.Count);
        }

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
        {
            returnInstanceDisposed(Disposed);
        }

        #endregion TestingInfrastructure
    }
}