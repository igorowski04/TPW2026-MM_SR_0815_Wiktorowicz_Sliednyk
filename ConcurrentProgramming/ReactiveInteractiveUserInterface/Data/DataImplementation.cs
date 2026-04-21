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
        public DataImplementation() { }
        #endregion ctor

        #region DataAbstractAPI

        public override void Start(int numberOfBalls, double width, double height, Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            MoveTimer?.Dispose();
            BallsList.Clear();

            for (int i = 0; i < numberOfBalls; i++)
            {
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
                    MoveTimer?.Dispose();
                    BallsList.Clear();
                }
                Disposed = true;
            }
            else
                throw new ObjectDisposedException(nameof(DataImplementation));
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region private

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

                for (int i = 0; i < BallsList.Count; i++)
                {
                    for (int j = i + 1; j < BallsList.Count; j++)
                    {
                        CheckCollision(BallsList[i], BallsList[j]);
                    }
                }

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

        private void CheckCollision(Ball b1, Ball b2)
        {
            double c1X = b1.Position.x + b1.Radius;
            double c1Y = b1.Position.y + b1.Radius;
            double c2X = b2.Position.x + b2.Radius;
            double c2Y = b2.Position.y + b2.Radius;

            double dx = c2X - c1X;
            double dy = c2Y - c1Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance == 0) return;

            if (distance <= b1.Radius + b2.Radius)
            {
                double nx = dx / distance;
                double ny = dy / distance;

                double relativeVelocityX = b2.Velocity.x - b1.Velocity.x;
                double relativeVelocityY = b2.Velocity.y - b1.Velocity.y;

                double dotProduct = relativeVelocityX * nx + relativeVelocityY * ny;

                if (dotProduct > 0)
                    return;

                double impulseX = nx * dotProduct;
                double impulseY = ny * dotProduct;

                b1.Velocity = new Vector(b1.Velocity.x + impulseX, b1.Velocity.y + impulseY);
                b2.Velocity = new Vector(b2.Velocity.x - impulseX, b2.Velocity.y - impulseY);
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