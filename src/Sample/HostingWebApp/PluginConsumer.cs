namespace HostingWebApp
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using Contracts;

	[Export]
	public class PluginConsumer
	{
		private readonly IEnumerable<IPlugin> _plugins;

		[ImportingConstructor]
		public PluginConsumer(IEnumerable<IPlugin> plugins)
		{
			_plugins = plugins;
		}
	}
}