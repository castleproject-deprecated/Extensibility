namespace WebApplication1
{
	using Castle.Extensibility;
	using Contracts;

	[BundleExport(typeof(IPlugin))]
	public class PluginB : IPlugin
	{

	}
}