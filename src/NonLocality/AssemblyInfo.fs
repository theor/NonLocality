﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("NonLocality")>]
[<assembly: AssemblyProductAttribute("NonLocality")>]
[<assembly: AssemblyDescriptionAttribute("S3 file sync")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
