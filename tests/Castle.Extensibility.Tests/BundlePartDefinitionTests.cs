namespace Castle.Extensibility.Tests
{
	using System.ComponentModel.Composition.Hosting;
	using System.ComponentModel.Composition.Primitives;
	using System.Linq;
	using Castle.Extensibility.Hosting;
	using Exts1;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class BundlePartDefinitionTests
	{
//		[Test]
//		public void MefAttributesAreIgnored()
//		{
//			var part = new BundlePartDefinition(new [] { typeof(Part1) });
//			part.ExportDefinitions.Count().Should().Be(0);
//			part.ImportDefinitions.Count().Should().Be(0);
//		}
//
//		[Test]
//		public void BundleExportsAttributes()
//		{
//			var part = new BundlePartDefinition(new[] { typeof(Part2) });
//			part.ExportDefinitions.Count().Should().Be(1);
//			part.ImportDefinitions.Count().Should().Be(0);
//
//			var def = part.ExportDefinitions.Single();
//			def.ContractName.Should().Be("Exts1.Part2");
//			def.Metadata.Count.Should().Be(1);
//			def.Metadata[CompositionConstants.ExportTypeIdentityMetadataName].Should().Be("Exts1.Part2");
//		}
//
//		[Test]
//		public void BundleExportsAttributes2()
//		{
//			var part = new BundlePartDefinition(new[] { typeof(Part3) });
//			part.ExportDefinitions.Count().Should().Be(1);
//			part.ImportDefinitions.Count().Should().Be(0);
//
//			var def = part.ExportDefinitions.Single();
//			def.ContractName.Should().Be("customname");
//			def.Metadata.Count.Should().Be(1);
//			def.Metadata[CompositionConstants.ExportTypeIdentityMetadataName].Should().Be("Exts1.Part3");
//		}
//
//		[Test]
//		public void BundleExportsAttributes3()
//		{
//			var part = new BundlePartDefinition(new[] { typeof(Part4) });
//			part.ExportDefinitions.Count().Should().Be(1);
//			part.ImportDefinitions.Count().Should().Be(0);
//
//			var def = part.ExportDefinitions.Single();
//			def.ContractName.Should().Be("System.IDisposable");
//			def.Metadata.Count.Should().Be(1);
//			def.Metadata[CompositionConstants.ExportTypeIdentityMetadataName].Should().Be("System.IDisposable");
//		}
//
//		[Test]
//		public void BundleImportsAttributes()
//		{
//			var part = new BundlePartDefinition(new[] { typeof(Part5) });
//			part.ExportDefinitions.Count().Should().Be(0);
//			part.ImportDefinitions.Count().Should().Be(1);
//
//			var def = part.ImportDefinitions.Single() as ContractBasedImportDefinition;
//			def.ContractName.Should().Be("System.String");
//			def.Cardinality.Should().Be(ImportCardinality.ExactlyOne);
//			def.RequiredTypeIdentity.Should().Be("System.String");
//		}
//
//		[Test]
//		public void BundleImportsAttributes2()
//		{
//			var part = new BundlePartDefinition(new[] { typeof(Part6) });
//			part.ExportDefinitions.Count().Should().Be(0);
//			part.ImportDefinitions.Count().Should().Be(1);
//
//			var def = part.ImportDefinitions.Single() as ContractBasedImportDefinition;
//			def.ContractName.Should().Be("Exts1.Part1");
//			def.Cardinality.Should().Be(ImportCardinality.ZeroOrMore);
//			def.RequiredTypeIdentity.Should().Be("Exts1.Part1");
//		}
//
//		[Test]
//		public void BundleImportsAttributes3()
//		{
//			var part = new BundlePartDefinition(new[] { typeof(Part7) });
//			part.ExportDefinitions.Count().Should().Be(0);
//			part.ImportDefinitions.Count().Should().Be(1);
//
//			var def = part.ImportDefinitions.Single() as ContractBasedImportDefinition;
//			def.ContractName.Should().Be("Exts1.Part1");
//		}
	}
}
