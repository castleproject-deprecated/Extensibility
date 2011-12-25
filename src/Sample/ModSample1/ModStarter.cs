using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModSample1
{
	using System.ComponentModel.Composition;
	using Castle.Extensibility;

	[Export(typeof(IModuleStarter))]
	public class ModStarter : IModuleStarter
	{
		public void Initialize(ModuleContext ctx)
		{
			
		}

		public void Terminate()
		{
		}
	}
}
