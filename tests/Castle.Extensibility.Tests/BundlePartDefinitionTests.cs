using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Castle.Extensibility.Tests
{
	using System.ComponentModel.Composition.Hosting;
	using System.ComponentModel.Composition.Primitives;
	using System.Linq;
	using Castle.Extensibility.Hosting;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class BundlePartDefinitionTests
	{
	    private static IEnumerable<ExportDefinition> _exports;
	    private static IEnumerable<ImportDefinition> _imports;

	    private static void CollectDefinitions(params Type[] types)
        {
            var result = BundlePartDefinitionBase.CollectBundleDefinitions(types);
            _exports = result.Item1;
            _imports = result.Item2;
        }

		[Test]
		public void MefAttributesAreIgnored()
		{
            CollectDefinitions(new[] { typeof(Part1) });
			_exports.Count().Should().Be(0);
			_imports.Count().Should().Be(0);
		}

		[Test]
		public void BundleExportsAttributes()
		{
			CollectDefinitions(new[] { typeof(Part2) });
			_exports.Count().Should().Be(1);
			_imports.Count().Should().Be(0);

			var def = _exports.Single();
            def.ContractName.Should().Be(typeof(Part2).FullName);
			def.Metadata.Count.Should().Be(1);
            def.Metadata[CompositionConstants.ExportTypeIdentityMetadataName].Should().Be(typeof(Part2).FullName);
		}

		[Test]
		public void BundleExportsAttributes2()
		{
			CollectDefinitions(new[] { typeof(Part3) });
			_exports.Count().Should().Be(1);
			_imports.Count().Should().Be(0);

			var def = _exports.Single();
			def.ContractName.Should().Be("customname");
			def.Metadata.Count.Should().Be(1);
            def.Metadata[CompositionConstants.ExportTypeIdentityMetadataName].Should().Be(typeof(Part3).FullName);
		}

		[Test]
		public void BundleExportsAttributes3()
		{
            CollectDefinitions(new[] { typeof(Part4) });
			_exports.Count().Should().Be(1);
			_imports.Count().Should().Be(0);

			var def = _exports.Single();
			def.ContractName.Should().Be("System.IDisposable");
			def.Metadata.Count.Should().Be(1);
			def.Metadata[CompositionConstants.ExportTypeIdentityMetadataName].Should().Be("System.IDisposable");
		}

		[Test]
		public void BundleImportsAttributes()
		{
			CollectDefinitions(new[] { typeof(Part5) });
			_exports.Count().Should().Be(0);
			_imports.Count().Should().Be(1);

			var def = _imports.Single() as ContractBasedImportDefinition;
			def.ContractName.Should().Be("System.String");
			def.Cardinality.Should().Be(ImportCardinality.ExactlyOne);
			def.RequiredTypeIdentity.Should().Be("System.String");
		}

		[Test]
		public void BundleImportsAttributes2()
		{
            CollectDefinitions(new[] { typeof(Part6) });
			_exports.Count().Should().Be(0);
			_imports.Count().Should().Be(1);

			var def = _imports.Single() as ContractBasedImportDefinition;
            def.ContractName.Should().Be(typeof(Part1).FullName);
			def.Cardinality.Should().Be(ImportCardinality.ZeroOrMore);
            def.RequiredTypeIdentity.Should().Be(typeof(Part1).FullName);
		}

		[Test]
		public void BundleImportsAttributes3()
		{
            CollectDefinitions(new[] { typeof(Part7) });
			_exports.Count().Should().Be(0);
			_imports.Count().Should().Be(1);

			var def = _imports.Single() as ContractBasedImportDefinition;
            def.ContractName.Should().Be(typeof(Part1).FullName);
		}

        [Test]
        public void BundleImportsAttributes4()
        {
            CollectDefinitions(new[] { typeof(Part8) });
            _exports.Count().Should().Be(0);
            _imports.Count().Should().Be(2);

            var def1 = _imports.ElementAt(1) as ContractBasedImportDefinition;
            def1.ContractName.Should().Be(typeof(Part2).FullName);
            var def2 = _imports.ElementAt(0) as ContractBasedImportDefinition;
            def2.ContractName.Should().Be(typeof(Part1).FullName);
        }

        [Export]
        public class Part1
        {
        }
        
        [BundleExport]
        public class Part2
        {
        }
        
        [BundleExport("customname")]
        public class Part3
        {
        }
        
        [BundleExport(typeof(IDisposable))]
        public class Part4
        {
        }
        
        public class Part5
        {
        	[BundleImport]
        	public string Testing { get; set; }
        }
        
        public class Part6
        {
        	[BundleImportMany]
        	public IEnumerable<Part1> Parts { get; set; }
        }
        
        public class Part7
        {
        	private readonly Part1 _part;
        
        	public Part7([BundleImport] Part1 part)
        	{
        		_part = part;
        	}
        }

        public class Part8
        {
            private readonly Part1 _part;
            private readonly Part2 _part2;

            public Part8([BundleImport] Part1 part)
            {
                _part = part;
            }

            public Part8([BundleImport] Part1 part, [BundleImport] Part2 part2)
            {
                _part = part;
                _part2 = part2;
            }
        }
	}
}
