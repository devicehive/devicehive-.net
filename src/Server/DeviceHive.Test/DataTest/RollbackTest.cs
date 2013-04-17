using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace DeviceHive.Test.DataTest
{
    public class RollbackTest
    {
        private TransactionScope _transaction;

        [SetUp]
        public virtual void SetUp()
        {
            _transaction = new TransactionScope();
        }

        [TearDown]
        public virtual void TearDown()
        {
            _transaction.Dispose();
        }
    }
}
