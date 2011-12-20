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

namespace Castle.Extensibility

    open System
    open System.ComponentModel.Composition
    open System.ComponentModel.Composition.Primitives
    open System.ComponentModel.Composition.ReflectionModel

    [<AllowNullLiteralAttribute>]
    type IAttributedImportDef = 
        interface
            abstract member ContractName : string
            abstract member ContractType : Type
            abstract member CreationPolicyReq : CreationPolicy
            abstract member Cardinality : ImportCardinality
        end

    [<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field ||| AttributeTargets.Parameter, 
        AllowMultiple = false, Inherited = false); AllowNullLiteral>]
    type BundleImportAttribute (contractName:string, contractType:Type) = 
        class
            inherit ImportAttribute(contractName, contractType)

            new () = 
                BundleImportAttribute(null, null)
            new (contractName) = 
                BundleImportAttribute(contractName, null)
            new (contractType) = 
                BundleImportAttribute(null, contractType)

            interface IAttributedImportDef with
                member x.ContractName = x.ContractName
                member x.ContractType = x.ContractType
                member x.CreationPolicyReq = x.RequiredCreationPolicy
                member x.Cardinality = if x.AllowDefault then ImportCardinality.ZeroOrOne else ImportCardinality.ExactlyOne
        end

    [<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field ||| AttributeTargets.Parameter, 
        AllowMultiple = false, Inherited = false); AllowNullLiteral>]
    type BundleImportManyAttribute (contractName:string, contractType:Type) = 
        class
            inherit ImportManyAttribute(contractName, contractType)

            new () = 
                BundleImportManyAttribute(null, null)
            new (contractName) = 
                BundleImportManyAttribute(contractName, null)
            new (contractType) = 
                BundleImportManyAttribute(null, contractType)

            interface IAttributedImportDef with
                member x.ContractName = x.ContractName
                member x.ContractType = x.ContractType
                member x.CreationPolicyReq = x.RequiredCreationPolicy
                member x.Cardinality = ImportCardinality.ZeroOrMore
        end


    [<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Property ||| AttributeTargets.Method, 
        AllowMultiple = true, Inherited = false); AllowNullLiteral>]
    type BundleExportAttribute (contractName:string, contractType:Type) = 
        class
            inherit ExportAttribute(contractName, contractType)

            new () = 
                BundleExportAttribute(null, null)
            new (contractType:Type) = 
                BundleExportAttribute(null, contractType)
            new (contractName:string) = 
                BundleExportAttribute(contractName, null)
        end
        
    [<Interface>]
    type IModuleStarter = 
        interface
            
            abstract member Initialize : unit -> unit

            abstract member Terminate : unit -> unit

        end