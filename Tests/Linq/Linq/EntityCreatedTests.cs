﻿using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Interceptors;
	using Model;

	[TestFixture]
	public class EntityCreatedTests : TestBase
	{
		ITestDataContext GetEntityCreatedContext(string configString, TestDataContextInterceptor interceptor)
		{
			interceptor.EntityCreatedCallCounter = 0;
			interceptor.CheckEntityIdentity      = false;
			interceptor.Parents.Clear();

			var ctx = GetDataContext(configString);
			ctx.AddInterceptor(interceptor);

			return ctx;
		}

		private class TestDataContextInterceptor : DataContextInterceptor
		{
			public int EntityCreatedCallCounter { get; set; }
			public bool CheckEntityIdentity     { get; set; }

			public Dictionary<int, Parent> Parents { get; } = new();

			public override object EntityCreated(DataContextEventData eventData, object entity)
			{
				if (CheckEntityIdentity && entity is Parent p)
				{
					if (Parents.TryGetValue(p.ParentID, out var pr))
						return entity;

					Parents[p.ParentID] = p;
				}

				EntityCreatedCallCounter++;
				return base.EntityCreated(eventData, entity);
			}
		}

		[Test]
		public void EntityCreatedTest0([DataSources] string configString)
		{
			using (var db = GetDataContext(configString))
			{
				var list = db.Parent.Take(5).ToList();
			}
		}

		[Test]
		public void EntityCreatedTest1([DataSources] string configString)
		{
			var interceptor = new TestDataContextInterceptor();
			using (var db = GetEntityCreatedContext(configString, interceptor))
			{
				var list = db.Parent.Take(5).ToList();

				Assert.That(interceptor.EntityCreatedCallCounter, Is.EqualTo(5));
			}
		}

		[Test]
		public void EntityCreatedTest2([DataSources] string configString)
		{
			var interceptor = new TestDataContextInterceptor();
			using (var db = GetEntityCreatedContext(configString, interceptor))
			{
				var list = db.Child.Select(c => new { c, c.Parent, a = new { c } }).Take(1).ToList();

				Assert.That(interceptor.EntityCreatedCallCounter, Is.EqualTo(2));
			}
		}

		[Test]
		public void EntityCreatedTest3([DataSources] string configString, [Values(false,true)] bool checkEntityIdentity)
		{
			var interceptor = new TestDataContextInterceptor();
			using (var db = GetEntityCreatedContext(configString, interceptor))
			{
				interceptor.CheckEntityIdentity = checkEntityIdentity;

				var list = db.Child.Where(c => c.Parent!.ParentID == 3).Select(c => c.Parent).ToList();

				Assert.That(interceptor.EntityCreatedCallCounter, Is.EqualTo(checkEntityIdentity ? 1 : 3));
			}
		}
	}
}
