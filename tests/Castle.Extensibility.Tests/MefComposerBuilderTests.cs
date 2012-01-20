namespace Castle.Extensibility.Tests
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.ComponentModel.Composition.Primitives;
	using System.Linq;
	using FluentAssertions;
	using Hosting;
	using NUnit.Framework;

	static class ImportDefinitionExt
	{
		public static ImportDefinition For<T>()
		{
			return new ContractBasedImportDefinition(
				AttributedModelServices.GetContractName(typeof (T)),
				AttributedModelServices.GetTypeIdentity(typeof (T)),
				new KeyValuePair<string, Type>[0], ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);
		}
	}

	[TestFixture]
	public class MefComposerBuilderTests
	{
		[Test]
		public void CPDisCreatedExposingASubsetOfPassedImports()
		{
			var builder = new MefComposerBuilder(new string[0]) as IComposablePartDefinitionBuilder;
			var context = new StubBindingContext(typeof(Part1));
			var mefCpd = builder.Build(context, 
				new ExportDefinition[] {  }, 
				new [] { ImportDefinitionExt.For<IDisposable>() }, 
				new Manifest("name", new Version(), null, ""), 
				new StubModuleContext(), 
				new IBehavior[0]);
			mefCpd.Should().NotBeNull();

			mefCpd.ExportDefinitions.Should().HaveCount(0);
			mefCpd.ImportDefinitions.Should().HaveCount(1);
			mefCpd.ImportDefinitions.ElementAt(0).ContractName.Should().Be(AttributedModelServices.GetContractName(typeof(IDisposable)));
		}

		[Test]
		public void CPDisCreatedExposingASubsetOfPassedExports()
		{
			var builder = new MefComposerBuilder(new string[0]) as IComposablePartDefinitionBuilder;
			var context = new StubBindingContext(typeof(Part1));
			var mefCpd = builder.Build(context,
				new []
					{
						new ExportDefinition(AttributedModelServices.GetContractName(typeof(Part1)), new Dictionary<string, object>()),
					},
				new ImportDefinition[] { },
				new Manifest("name", new Version(), null, ""),
				new StubModuleContext(),
				new IBehavior[0]);
			mefCpd.Should().NotBeNull();

			mefCpd.ExportDefinitions.Should().HaveCount(1);
			mefCpd.ImportDefinitions.Should().HaveCount(0);
			mefCpd.ExportDefinitions.ElementAt(0).ContractName.Should().Be(AttributedModelServices.GetContractName(typeof(Part1)));
		}

		[Export]
		class Part1
		{
			[Import]
			public IDisposable Import1 { get; set; }
			[Import]
			public IServiceProvider Import2 { get; set; }
		}
	}
}
