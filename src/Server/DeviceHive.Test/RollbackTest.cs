using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace DeviceHive.Test
{
    public class RollbackTest
    {
        private TransactionScope _transaction;

        [SetUp]
        public void SetUp()
        {
            _transaction = new TransactionScope();
        }

        [TearDown]
        public void TearDown()
        {
            _transaction.Dispose();
        }
    }
}
