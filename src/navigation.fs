(** Custom navigation
------
This port of the Elm library is about treating the address bar as an input to your program.
It converts changes in the address bar into messages and provides functions for manipulation of the browser history.
*)
namespace Elmish.Navigation

open Browser
open Browser.Types
open Elmish

(**
#### Parser
A function to turn the string in the address bar into data that is easier for your app to handle.
*)
type Parser<'a> = Location -> 'a

type Navigable<'msg> =
    | Change of Location
    | UserMsg of 'msg

(**
#### Direct history manipulation
*)
[<RequireQualifiedAccess>]
module Navigation =
    let [<Literal>] internal NavigatedEvent = "NavigatedEvent"

    /// Modify current location
    let modifyUrl (newUrl:string):Cmd<_> =
        [fun _ -> history.replaceState((), "", newUrl)]

    /// Push new location into history and navigate there
    let newUrl (newUrl:string):Cmd<_> =
        [fun _ -> history.pushState((), "", newUrl)
                  let ev = CustomEvent.Create(NavigatedEvent)
                  window.dispatchEvent ev
                  |> ignore ]

    /// Jump to some point in history (positve=forward, nagative=backward)
    let jump (n:int):Cmd<_> =
        [fun _ -> history.go n]


(**
#### Program extensions
Treat user's program as a child component, by wrapping it and handling navigation events.
*)
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Program =

    /// Add the navigation to a program made with `mkProgram` or `mkSimple`.
    /// urlUpdate: similar to `update` function, but receives parsed url instead of message as an input.
    let toNavigable (parser : Parser<'a>)
                    (urlUpdate : 'a->'model->('model * Cmd<'msg>))
                    (program : Program<'arg,'model,'msg,'view>) =

        let onChangeRef : (Event -> unit) ref =
            fun _ ->
                failwith "`onChangeRef` has not been initialized.\nThis should not happen, please open an issue if the problem persist"
            |> ref

        let map (model, cmd) =
            model, cmd |> Cmd.map UserMsg

        let update userUpdate msg model =
            match msg with
            | Change location ->
                urlUpdate (parser location) model
            | UserMsg userMsg ->
                userUpdate userMsg model
            |> map

        let onLocationChange (dispatch:Dispatch<_ Navigable>) =
            let mutable lastLocation = None
            let onChange _ =
                match lastLocation with
                | Some href when href = window.location.href -> ()
                | _ ->
                    lastLocation <- Some window.location.href
                    Change window.location |> dispatch

            onChangeRef.Value <- onChange

            window.addEventListener("popstate", onChangeRef.Value)
            window.addEventListener("hashchange", onChangeRef.Value)
            window.addEventListener(Navigation.NavigatedEvent, onChangeRef.Value)
            { new System.IDisposable with
                member _.Dispose () = 
                    window.removeEventListener("popstate", onChangeRef.Value)
                    window.removeEventListener("hashchange", onChangeRef.Value)
                    window.removeEventListener(Navigation.NavigatedEvent, onChangeRef.Value)
            }

        let subs userSubscribe model =
            Sub.batch
                [ [ ["onLocationChange"], onLocationChange ]
                  userSubscribe model |> Sub.map "user" UserMsg ]

        let init userInit () =
            userInit (parser window.location) |> map

        let setState userSetState model dispatch =
            userSetState model (UserMsg >> dispatch)

        let view userView model dispatch =
            userView model (UserMsg >> dispatch)

        let termination (predicate,terminate) =
            let predicate' =
                function
                | UserMsg msg -> predicate msg
                | _ -> false

            let terminate' model =
                window.removeEventListener("popstate", onChangeRef.Value)
                window.removeEventListener("hashchange", onChangeRef.Value)
                window.removeEventListener(Navigation.NavigatedEvent, onChangeRef.Value)

                terminate model

            predicate', terminate'

        program
        |> Program.map init update view setState subs termination
