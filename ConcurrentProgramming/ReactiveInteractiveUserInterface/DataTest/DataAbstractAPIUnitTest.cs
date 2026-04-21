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

namespace TP.ConcurrentProgramming.Data.Test
{
    [TestClass]
    public class DataAbstractAPIUnitTest
    {   

        [TestMethod]
        /// |======================================|
        /// |-=- WIELE INSTANCJI WARSTWY DANYCH -=-|
        /// |======================================|
        /// | - Sprawdza, czy za razem tworzona jest nowa instancja warstwy danych. W celach testowych.
        /// | Wcześniej był Singleton, który gwarantował, że zawsze będziemy mieć jedną instancję - niepraktyczne.
        public void ConstructorTestTestMethod()
        {
            DataAbstractAPI instance1 = DataAbstractAPI.GetDataLayer();
            DataAbstractAPI instance2 = DataAbstractAPI.GetDataLayer();

            // | - AreEqual: czy ma taką samą wartość w środku, | - AreSame - czy taka sama referencja
            Assert.AreNotSame(instance1, instance2);

            instance1.Dispose();
            instance2.Dispose();
        }
    }
}