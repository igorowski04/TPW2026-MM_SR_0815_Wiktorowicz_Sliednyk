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
using TP.ConcurrentProgramming.BusinessLogic;

namespace TP.ConcurrentProgramming.BusinessLogic.Test
{
    [TestClass]
    public class BusinessLogicAbstractAPIUnitTest
    {
        [TestMethod]
        // |==================================================================================|
        // |-=- POPRAWNA INICJALIZACJA KONSTRUKTORA, KTÓRY ZWRACA INSTANCJĘ WARSTWY LOGIKI -=-|
        // |==================================================================================|
        public void BusinessLogicConstructorTestMethod()
        {
            BusinessLogicAbstractAPI instance1 = BusinessLogicAbstractAPI.GetBusinessLogicLayer();
            BusinessLogicAbstractAPI instance2 = BusinessLogicAbstractAPI.GetBusinessLogicLayer();
            Assert.AreNotSame(instance1, instance2);
            instance1.Dispose();
            instance2.Dispose();
        }


    }
}