using System;

namespace HostingWebApp
{
	using System.ComponentModel.Composition.Hosting;
	using Castle.Extensibility.Hosting;

	public class Global : System.Web.HttpApplication
	{
		private static HostingContainer _hosting;

		protected void Application_Start(object sender, EventArgs e)
		{
			var catalog = new AssemblyCatalog(typeof(Global).Assembly);
			_hosting = new HostingContainer("bundles", catalog);

		}
	}
}