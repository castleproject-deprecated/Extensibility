namespace Castle.Extensibility.Tests
{
	using System;
	using System.ComponentModel.Composition.Primitives;
	using System.IO;
	using System.Linq;
	using FluentAssertions;
	using Hosting;
	using NUnit.Framework;

	[TestFixture]
	public class DefinitionsCacheReaderTestCase
	{
		private readonly string _folder = null;

		public DefinitionsCacheReaderTestCase()
		{
			_folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "manifests");
		}

		[Test]
		public void BuildManifest_ForEmptyFile_Succeeds()
		{
			using (var fs = new FileStream(Path.Combine(_folder, "manifest-generated1.xml"), FileMode.Open))
			{
				var definitions = DefinitionsCacheReader.build_manifest(fs, _folder, new StubBindingContext());

				definitions.Exports.Should().BeEmpty();
				definitions.Imports.Should().BeEmpty();
			}
		}

		[Test]
		public void BuildManifest_ForExport_Succeeds()
		{
			using (var fs = new FileStream(Path.Combine(_folder, "manifest-generated2.xml"), FileMode.Open))
			{
				var definitions = DefinitionsCacheReader.build_manifest(fs, _folder, new StubBindingContext());

				definitions.Exports.Should().HaveCount(1);
				definitions.Imports.Should().BeEmpty();

				var exportDef = definitions.Exports.ElementAt(0);
				exportDef.ContractName.Should().Equals("Name");
				exportDef.Metadata.Count.Should().Be(0);
			}
		}

		[Test]
		public void BuildManifest_ForExportWithMetadata_Succeeds()
		{
			using (var fs = new FileStream(Path.Combine(_folder, "manifest-generated3.xml"), FileMode.Open))
			{
				var definitions = DefinitionsCacheReader.build_manifest(fs, _folder, new StubBindingContext());

				definitions.Exports.Should().HaveCount(1);
				definitions.Imports.Should().BeEmpty();

				var exportDef = definitions.Exports.ElementAt(0);
				exportDef.ContractName.Should().Equals("Name");
				exportDef.Metadata.Count.Should().Be(1);
				exportDef.Metadata["key1"].Should().Be("This is a metadata value");
			}
		}

		[Test]
		public void BuildManifest_TypeAsMetadataEntry_UsesBindingContext()
		{
			using (var fs = new FileStream(Path.Combine(_folder, "manifest-generated4.xml"), FileMode.Open))
			{
				var definitions = DefinitionsCacheReader.build_manifest(fs, _folder, new StubBindingContext(typeof(DummyDisposable)));

				definitions.Exports.Should().HaveCount(1);
				definitions.Imports.Should().BeEmpty();

				var exportDef = definitions.Exports.ElementAt(0);
				exportDef.Metadata["key1"].Should().Be(typeof(DummyDisposable));
			}
		}

		[Test]
		public void BuildManifest_ForImport_Succeeds()
		{
			using (var fs = new FileStream(Path.Combine(_folder, "manifest-generated5.xml"), FileMode.Open))
			{
				var definitions = DefinitionsCacheReader.build_manifest(fs, _folder, new StubBindingContext());

				definitions.Exports.Should().BeEmpty();
				definitions.Imports.Should().HaveCount(1);

				var importDef = definitions.Imports.ElementAt(0);
				importDef.ContractName.Should().Equals("SomeName");
				importDef.Metadata.Count.Should().Be(0);
			}
		}

		[Test]
		public void BuildManifest_ForImportWithCardinality_Succeeds()
		{
			using (var fs = new FileStream(Path.Combine(_folder, "manifest-generated6.xml"), FileMode.Open))
			{
				var definitions = DefinitionsCacheReader.build_manifest(fs, _folder, new StubBindingContext());

				definitions.Exports.Should().BeEmpty();
				definitions.Imports.Should().HaveCount(1);

				var importDef = definitions.Imports.ElementAt(0);
				importDef.ContractName.Should().Equals("SomeName");
				importDef.Metadata.Count.Should().Be(0);
				importDef.Cardinality.Should().Be(ImportCardinality.ExactlyOne);
			}
		}
	}
}
