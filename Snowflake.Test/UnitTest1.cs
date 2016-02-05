using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snowflake.Lite;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Snowflake.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var factory = new IdFactory();
            var actual = new ConcurrentBag<long>();

            Debug.WriteLine("DataCenterId: {0}", factory.DataCenterId);
            Debug.WriteLine("WorkerId: {0}", factory.WorkerId);

            Parallel.ForEach(Enumerable.Repeat(new Action<long>(actual.Add), 100).ToArray(), add =>
            {
                add(factory.GetNextId());
            });

            CollectionAssert.AllItemsAreUnique(actual);
        }
    }
}
