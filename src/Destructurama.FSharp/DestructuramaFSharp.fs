﻿// Copyright 2015 Destructurama Contributors, Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Destructurama.FSharp

open Microsoft.FSharp.Reflection
open Serilog.Core
open Serilog.Events
open System

// Based on the sample from @vlaci in https://github.com/serilog/serilog/issues/352

type public FSharpTypesDestructuringPolicy() =
    interface Serilog.Core.IDestructuringPolicy with
        member this.TryDestructure(value,
                                   propertyValueFactory : ILogEventPropertyValueFactory,
                                   result: byref<LogEventPropertyValue>) =
            let valueType = value.GetType()
            if valueType.IsConstructedGenericType && valueType.GetGenericTypeDefinition() = typedefof<List<_>> then
                let elems = value :?> obj seq
                            |> Seq.map(fun v -> propertyValueFactory.CreatePropertyValue(v, true))
                result <- SequenceValue(elems)
                true
            else if FSharpType.IsUnion valueType then
                let case, fields = FSharpValue.GetUnionFields(value, valueType)

                let properties = Seq.zip (case.GetFields()) fields
                                 |> Seq.map(fun (n, v) -> LogEventProperty(
                                                            n.Name,
                                                            propertyValueFactory.CreatePropertyValue(v, true)))

                result <- StructureValue(properties, case.Name)
                true
            else
                false

namespace Serilog

open Serilog.Configuration
open Destructurama.FSharp

[<AutoOpen>]
module public LoggerDestructuringConfigurationExtensions =
    type public LoggerDestructuringConfiguration with
        member public this.FSharpTypes() =
            this.With<FSharpTypesDestructuringPolicy>()

