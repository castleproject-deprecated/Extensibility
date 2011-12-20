namespace Castle.Extensibility.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.ComponentModel.Composition;
	using System.ComponentModel.Composition.Hosting;
	using Castle.Extensibility.Hosting;
	using FluentAssertions;
	using NUnit.Framework;
	using System.IO;

	[Export]
	public class Something
	{
		private readonly IEnumerable<IServiceProvider> _providers;
		
		[ImportingConstructor]
		public Something([ImportMany] IEnumerable<IServiceProvider> providers)
		{
			_providers = providers;
		}

		public IEnumerable<IServiceProvider> Providers
		{
			get { return _providers; }
		}
	}

	[TestFixture]
	public class HostingTests
	{
		[Test]
		public void aaaaa()
		{
			var dir = AppDomain.CurrentDomain.BaseDirectory;
			var cat = new BundleCatalog(dir);
			var host = new HostingContainer(new[] { cat }, new TypeCatalog(typeof(Something)));
			var something = host.GetExportedValue<Something>();
			something.Providers.Count().Should().Be(1);
		}
	}
}
