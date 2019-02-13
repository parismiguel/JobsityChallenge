using Chat.Web.Helpers;
using Chat.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Chat.Web.Tests
{
    [TestClass]
    public class ApisTest
    {
        [TestMethod]
        public void GestStockTest()
        {
            Response output = new Response();

            try
            {
                output = Apis.GestStock();

            }
            catch (Exception e)
            {
                StringAssert.Contains(e.Message, "Error al ejecutar el método");
                return;
            }

            Assert.AreEqual(true, output.Status);
        }
    }
}
