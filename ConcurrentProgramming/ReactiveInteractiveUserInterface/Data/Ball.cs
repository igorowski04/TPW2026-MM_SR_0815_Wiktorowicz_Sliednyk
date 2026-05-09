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
        // ================================
        // -=- Zmienne razem z getterem -=-
        // ================================

        private IVector _position;
        public event EventHandler<IVector>? NewPositionNotification;

        // Promień i prędkość kuli
        public double Radius { get; }
        public double Mass { get; }
        public IVector Velocity { get; set; }
        
        // ten fragment pozwala przypisać nową wartość pozycji kuli przy pomocy set.
        // Kiedy pozycja się zaktualizuje, reszta warstw zostanie powiadomiona o zmianie
        // .Invoke(kto nadaje, komunikat[zmiana pozycji u nas]) - powiadamia inne warstwy,
        //  że pozycja została zaktualizowana
        public IVector Position
        {
            get => _position;
            set
            {
                _position = value;
                NewPositionNotification?.Invoke(this, _position);
            }
        }

        // ===================
        // -=- Konstruktor -=-
        // ===================
        internal Ball(IVector initialPosition, IVector initialVelocity, double radius, double mass)
        {
            _position = initialPosition;
            Velocity = initialVelocity;
            Radius = radius;
            Mass = mass;
        }
    }
}