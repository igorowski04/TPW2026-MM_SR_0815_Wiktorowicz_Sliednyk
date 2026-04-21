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

namespace TP.ConcurrentProgramming.Data
{
    internal class Ball : IBall
    {
        #region ctor

        internal Ball(Vector initialPosition, Vector initialVelocity, double radius, double boardWidth, double boardHeight)
        {
            Position = initialPosition;
            Velocity = initialVelocity;
            Radius = radius;
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
        }

        #endregion ctor

        #region IBall

        public event EventHandler<IVector>? NewPositionNotification;

        public IVector Velocity { get; set; }

        #endregion IBall

        #region private

        internal Vector Position { get; private set; }
        internal double Radius { get; }

        private readonly double BoardWidth;
        private readonly double BoardHeight;

        private void RaiseNewPositionChangeNotification()
        {
            NewPositionNotification?.Invoke(this, Position);
        }

        internal void Move(Vector delta)
        {
            double newX = Position.x + delta.x;
            double newY = Position.y + delta.y;
            double newVx = Velocity.x;
            double newVy = Velocity.y;

            if (newX <= 0)
            {
                newX = 0;
                newVx = -newVx;
            }
            else if (newX + (Radius * 2) >= BoardWidth)
            {
                newX = BoardWidth - (Radius * 2);
                newVx = -newVx;
            }

            if (newY <= 0)
            {
                newY = 0;
                newVy = -newVy;
            }
            else if (newY + (Radius * 2) >= BoardHeight)
            {
                newY = BoardHeight - (Radius * 2);
                newVy = -newVy;
            }

            Position = new Vector(newX, newY);
            Velocity = new Vector(newVx, newVy);

            RaiseNewPositionChangeNotification();
        }

        #endregion private
    }
}