namespace Exts1
{
	using System;
	using Castle.Extensibility;

	[BundleExport(typeof(IServiceProvider))]
	public class ServProvider1 : IServiceProvider
	{
		public object GetService(Type serviceType)
		{
			throw new NotImplementedException();
		}
	}
}
