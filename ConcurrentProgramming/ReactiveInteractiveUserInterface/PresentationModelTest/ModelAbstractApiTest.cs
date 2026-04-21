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
using TP.ConcurrentProgramming.Presentation.Model;

namespace TP.ConcurrentProgramming.PresentationModelTest
{
    [TestClass]
    public class ModelAbstractAPITest
    {
        [TestMethod]
        public void ConstructorTestMethod() 
        {
            ModelAbstractApi instance1 = ModelAbstractApi.CreateModel();
            ModelAbstractApi instance2 = ModelAbstractApi.CreateModel();

            // Zmieniono na AreNotSame, bo każda nowa warstwa to czysta instancja
            Assert.AreNotSame(instance1, instance2);
        }
    }
}