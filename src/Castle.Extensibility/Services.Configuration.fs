//  Copyright 2004-2011 Castle Project - http://www.castleproject.org/
//  Hamilton Verissimo de Oliveira and individual contributors as indicated. 
//  See the committers.txt/contributors.txt in the distribution for a 
//  full listing of individual contributors.
// 
//  This is free software; you can redistribute it and/or modify it
//  under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 3 of
//  the License, or (at your option) any later version.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this software; if not, write to the Free
//  Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
//  02110-1301 USA, or see the FSF site: http://www.fsf.org.

namespace Castle.Extensibility.Services.Configuration

    open System
    open System.IO
    open System.Linq
    open System.Reflection
    open System.Threading
    open System.Collections.Generic
    open System.Configuration
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Hosting
    open System.ComponentModel.Composition.Primitives
    open Castle.Extensibility

    [<AllowNullLiteral>]
    type IConfiguration = 
        interface
            abstract member GetConnectionString : name:string -> string
            abstract member GetConnectionString : unit -> string
            abstract member Settings : IDictionary<string,string>
        end

    [<AllowNullLiteral>]
    type IConfigurationService = 
        interface
            abstract member GetConfig : unit -> IConfiguration
            abstract member GetConfig : name:string -> IConfiguration
        end

    [<AllowNullLiteral>]
    type DotNetBasedConfigService(bundleName:string, storageFolder:string) = 

        // let getStorage(scope:string) : IStorage = 
        //    let path = Path.Combine (safeStorageFolder, Path.Combine(bundleName, scope))
        //    upcast FSStorage(path)

        interface IConfigurationService with 

            member x.GetConfig() = 
                let map = ExeConfigurationFileMap()
                map.ExeConfigFilename <- Path.Combine (storageFolder, Path.Combine(bundleName, "config\\bundle.config"))
                let cfg = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None)
                upcast DotNetBasedConfig(cfg)
        
            member x.GetConfig(name) = 
                let map = ExeConfigurationFileMap()
                map.ExeConfigFilename <- Path.Combine (storageFolder, Path.Combine(bundleName, "config\\" + name + ".config"))
                let cfg = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None)
                upcast DotNetBasedConfig(cfg)
                

    and DotNetBasedConfig(config) = 
        
        let _settings = 
            let setts = config.AppSettings.Settings
            Enumerable.ToDictionary(setts.AllKeys, (fun k -> k), (fun k -> setts.[k].Value ))

        interface IConfiguration with 
            
            member x.GetConnectionString() =
                (x :> IConfiguration).GetConnectionString("default")

            member x.GetConnectionString(name) = 
                let settings = config.ConnectionStrings.ConnectionStrings.Item(name)
                if settings <> null then settings.ConnectionString else null
                
            member x.Settings = upcast _settings