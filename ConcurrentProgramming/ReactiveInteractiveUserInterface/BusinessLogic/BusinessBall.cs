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
using DataBall = TP.ConcurrentProgramming.Data.IBall;

namespace TP.ConcurrentProgramming.BusinessLogic
{
    internal class BusinessBall : IBall
    {
        // ===============
        // -=- ZMIENNE -=-
        // ===============
        private readonly DataBall _dataBall;
        public event EventHandler<IPosition>? NewPositionNotification;
        public double Radius => _dataBall.Radius;



        public BusinessBall(DataBall dataBall)
        {
            _dataBall = dataBall;
            
            // wzorzec obserwatora
            _dataBall.NewPositionNotification += DataBall_NewPositionNotification;
        }
            
        // Funckja tłumacząca wekror z warstwy danych, na pozycję
        private void DataBall_NewPositionNotification(object? sender, TP.ConcurrentProgramming.Data.IVector e)
        {
            NewPositionNotification?.Invoke(this, new Position(e.X, e.Y));
        }

    }
}