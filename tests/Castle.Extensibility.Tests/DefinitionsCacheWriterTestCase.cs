namespace Castle.Extensibility.Tests
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.ComponentModel.Composition.Primitives;
	using System.IO;
	using System.Linq;
	using FluentAssertions;
	using Hosting;
	using NUnit.Framework;

	[TestFixture]
	public class DefinitionsCacheWriterTestCase
	{
		[Test]
		public void WriteManifest_ForEmptyDefinitions_Succeeds()
		{
			var writer = new StringWriter();
			DefinitionsCacheWriter.write_manifest(writer, new ExportDefinition[0], new ImportDefinition[0]);
			writer.GetStringBuilder().ToString().Should().Be(
@"<?xml version=""1.0"" encoding=""utf-16""?>
<manifest>
  <exports />
  <imports />
</manifest>");
		}

		[Test]
		public void WriteManifest_ForExport_Succeeds()
		{
			var writer = new StringWriter();
			DefinitionsCacheWriter.write_manifest(writer, 
				new[] { new ExportDefinition("contractName", new Dictionary<string, object>()) }, 
				new ImportDefinition[0]);

			writer.GetStringBuilder().ToString().Should().Be(
@"<?xml version=""1.0"" encoding=""utf-16""?>
<manifest>
  <exports>
    <export>
      <contract>contractName</contract>
    </export>
  </exports>
  <imports />
</manifest>");
		}

		[Test]
		public void WriteManifest_ForExportWithMetadata_Succeeds()
		{
			var writer = new StringWriter();
			var metadata = new Dictionary<string, object>();
			metadata["mykey"] = "simple text";
			metadata["v"] = 100;
			metadata["identity"] = typeof (DefinitionsCacheWriterTestCase);

			DefinitionsCacheWriter.write_manifest(writer,
				new[] { new ExportDefinition("contractName", metadata) },
				new ImportDefinition[0]);

			writer.GetStringBuilder().ToString().Should().Be(
@"<?xml version=""1.0"" encoding=""utf-16""?>
<manifest>
  <exports>
    <export>
      <contract>contractName</contract>
      <metadata>
        <entry key=""mykey"" type=""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"">simple text</entry>
        <entry key=""v"" type=""System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"">100</entry>
        <entry key=""identity"" type=""System.Type, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"">Castle.Extensibility.Tests.DefinitionsCacheWriterTestCase, Castle.Extensibility.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</entry>
      </metadata>
    </export>
  </exports>
  <imports />
</manifest>");

		}

		[Test]
		public void WriteManifest_ForImport_Succeeds()
		{
			var writer = new StringWriter();
			DefinitionsCacheWriter.write_manifest(writer,
				new ExportDefinition[0],
				new [] { new ContractBasedImportDefinition("Name", 
					"typeIdentity", Enumerable.Empty<KeyValuePair<string,Type>>(), ImportCardinality.ZeroOrOne, true, false, CreationPolicy.Any) });

			writer.GetStringBuilder().ToString().Should().Be(
@"<?xml version=""1.0"" encoding=""utf-16""?>
<manifest>
  <exports />
  <imports>
    <import>
      <contract>Name</contract>
      <cardinality>ZeroOrOne</cardinality>
    </import>
  </imports>
</manifest>");
		}
	}
}
