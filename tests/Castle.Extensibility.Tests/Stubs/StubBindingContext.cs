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
			return _types.Where(t => t.Name == name).FirstOrDefault();
		}
	}
}