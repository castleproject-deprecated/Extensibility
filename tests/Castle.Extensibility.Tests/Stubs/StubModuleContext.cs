namespace Castle.Extensibility.Tests
{
	using System;

	class StubModuleContext : ModuleContext
	{
		public override bool HasService<T>()
		{
			throw new NotImplementedException();
		}

		public override T GetService<T>()
		{
			throw new NotImplementedException();
		}
	}
}