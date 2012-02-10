namespace Castle.Extensibility.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	class StubBindingContext : IBindingContext
	{
		private readonly Type[] _types;

		public StubBindingContext(params Type[] types)
		{
			_types = types;
		}

		public IEnumerable<Type> GetAllTypes()
		{
			return _types;
		}

		public Type GetContextType(string name)
		{
			if (name.Contains(","))
			{
				var typeName = name.Substring(0, name.IndexOf(','));
				return _types.Where(t => t.FullName.StartsWith(typeName)).FirstOrDefault();	
			}
			else
				return _types.Where(t => t.FullName.StartsWith(name)).FirstOrDefault();
		}
	}

	class DummyDisposable : IDisposable
	{
		public void Dispose()
		{
			
		}
	}
}