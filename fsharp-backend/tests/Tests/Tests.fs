module Tests.All

// Main entry point for tests being run

open Expecto
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks

let tests =
  testList
    "tests"
    [ Tests.LibExecution.tests
      Tests.LibBackend.tests
      Tests.BwdServer.tests
      Tests.ApiServer.tests ]

[<EntryPoint>]
let main args =
  let (_ : Task) = Tests.BwdServer.init ()
  LibBackend.ProgramSerialization.OCamlInterop.Binary.init ()
  LibBackend.Migrations.init ()
  (LibBackend.Account.initTestAccounts ()).Wait()

  // this does async stuff within it, so do not run it from a task/async
  // context or it may hang
  runTestsWithCLIArgs [] args tests
