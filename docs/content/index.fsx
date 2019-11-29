(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../src/bin/Release/netstandard2.0"
#r "Fable.Elmish.Browser.dll"
open Elmish

(**
Routing and navigation for browser apps
=======

This library builds on [elmish-urlParser](https://elmish.github.io/urlParser) to 
handle browser address changes and manipulate browser history directly.


## Installation

```shell
paket add nuget Fable.Elmish.Browser
```

## Routing
Intercept browser address changes and feed them into the dispatch loop.

Usage:
*)

open Elmish.Navigation

Program.mkProgram init update view
|> Program.toNavigable parser urlUpdate
|> Program.run

(**
For more information see the [routing tutorial](routing.html).

## Navigation
Manipulate the browser's navigation and history.

Usage:
*)

open Elmish.Navigation

let update model msg =
    model, Navigation.newUrl "some_other_location"

(**

For details see the [`Navigation`](navigation.html##Direct-history-manipulation) module.

*)

