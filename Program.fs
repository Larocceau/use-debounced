open Browser
open System.Threading
open Feliz
open Fetch

module React =
    let useDebounced (delay: int) f =
        // this value is only used to affect the behavior of dispatchUpdate where it's accessed via the updater, so can be discarded here
        let _, updateCancellationSource =
            React.useStateWithUpdater (None: CancellationTokenSource option)

        fun args ->
            let cts = new CancellationTokenSource()

            updateCancellationSource (fun maybeExistingCts ->
                match maybeExistingCts with
                | None -> ()
                | Some existingCts -> existingCts.Cancel()

                Some cts)

            async {
                do! Async.Sleep delay


                if not cts.IsCancellationRequested then
                    f args

                cts.Dispose()
            }
            |> Async.Start

let searchJokes searchTerm : Fable.Core.JS.Promise<string option> =
    promise {
        let! result = tryFetch $"https://v2.jokeapi.dev/joke/Any?format=txt&contains={searchTerm}" []

        match result with
        | Ok response ->
            let! text = response.text ()
            return Some text
        | Error _ -> return None
    }

[<ReactComponent>]
let Joker () =

    let (joke, setJoke) = React.useState (Some "Search to find a funny joke!")

    // let updateJoke searchTerm =
    //     searchJokes searchTerm |> Promise.iter setJoke

    let updateJoke = React.useDebounced 500 (searchJokes >> Promise.iter setJoke)

    React.fragment
        [

          Html.form
              [ Html.label [ prop.text "Search for a joke!" ]
                Html.input [ prop.onChange (fun (text: string) -> updateJoke text) ] ]

          Html.p (
              match joke with
              | Some j -> j
              | None -> "No jokes found! please try again!"
          ) ]


let page =
    React.fragment
        [ Html.header
              [ Html.h1 "Jokes galore"

                ]
          Html.main [ Joker() ]
          Html.footer
              [ Html.p
                    [ Html.text "Jokes galore is powered by "
                      Html.a [ prop.text "JokeApi"; prop.href "https://jokeapi.dev/" ] ]

                ] ]

ReactDOM.render (page, document.getElementById "app")
