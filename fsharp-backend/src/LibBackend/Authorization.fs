module LibBackend.Authorization

// Permission levels, scoped to a single owner.

// These are stored in the datastore for granted access,
// or returned by the functions in this namespace to indicate a
// particular user's permissions for a particular auth_domain.
//
// We often use `permission option' to include the case where
// a user has no special access to a particular auth_domain.

module Account = LibBackend.Account

open System.Threading.Tasks
open FSharp.Control.Tasks
open Prelude
open Tablecloth

type Permission =
  | Read
  | ReadWrite

  member this.max(other : Permission) : Permission =
    match this, other with
    | ReadWrite, _
    | _, ReadWrite -> ReadWrite
    | Read, _
    | _, Read -> Read

  static member parse(str : string) : Permission =
    match str with
    | "r" -> Read
    | "rw" -> ReadWrite
    | _ -> failwith "couldn't decode permission"

  override this.ToString() : string =
    match this with
    | Read -> "r"
    | ReadWrite -> "rw"

// let set_user_access (user : Uuidm.t) (org : Uuidm.t) (p : permission option) :
//     unit =
//   match p with
//   | None ->
//       Db.run
//         ~name:"set_user_access"
//         "DELETE from access
//         WHERE access.access_account = $1 AND access.organization_account = $2"
//         ~params:[Uuid user; Uuid org] ;
//       ()
//   | Some p ->
//       Db.run
//         ~name:"set_user_access"
//         "INSERT into access
//         (access_account, organization_account, permission)
//         VALUES
//         ($1, $2, $3)
//         ON CONFLICT (access_account, organization_account) DO UPDATE SET permission = EXCLUDED.permission"
//         ~params:[Uuid user; Uuid org; permission_to_db p] ;
//       ()
//
//
// (* Returns a list of (username, permission) pairs for a given auth_domain,
//  * denoting who has been granted access to a given domain *)
// let grants_for ~auth_domain : (Account.username * permission) list =
//   Db.fetch
//     ~name:"fetch_grants"
//     "SELECT user_.username, permission FROM access
//      INNER JOIN accounts user_ on access.access_account = user_.id
//      INNER JOIN accounts org on access.organization_account = org.id
//      WHERE org.username = $1"
//     ~params:[String auth_domain]
//   |> List.map ~f:(fun l ->
//          match l with
//          | [username; db_perm] ->
//              (username, permission_of_db db_perm)
//          | _ ->
//              Exception.internal
//                "bad format from Authorization.grants_for#fetch_grants")
//
//
// (* Returns a list of (organization name, permission) pairs for a given username,
//  * denoting which organizations the user has been granted permissions towards *)
// let orgs_for ~(username : Account.username) : (string * permission) list =
//   Db.fetch
//     ~name:"fetch_orgs"
//     "SELECT org.username, permission
//      FROM access
//      INNER JOIN accounts user_ on access.access_account = user_.id
//      INNER JOIN accounts org on access.organization_account = org.id
//      WHERE user_.username = $1"
//     ~params:[String username]
//   |> List.map ~f:(fun l ->
//          match l with
//          | [org; db_perm] ->
//              (org, permission_of_db db_perm)
//          | _ ->
//              Exception.internal
//                "bad format from Authorization.grants_for#fetch_orgs")
//
//
// (* If a user has a DB row indicating granted access to this auth_domain,
//    find it. *)
// let granted_permission ~(username : Account.username) ~(auth_domain : string) :
//     permission option =
//   Db.fetch
//     ~name:"check_access"
//     "SELECT permission FROM access
//        INNER JOIN accounts user_ ON access.access_account = user_.id
//        INNER JOIN accounts org ON access.organization_account = org.id
//        WHERE org.username = $1 AND user_.username = $2"
//     ~params:[String auth_domain; String username]
//   |> List.hd
//   |> Option.bind ~f:List.hd
//   |> Option.map ~f:permission_of_db
//
//
// (* If a user is an admin they get write on everything. *)
// let admin_permission ~(username : Account.username) =
//   if Account.is_admin ~username then Some ReadWrite else None


// We special-case some users, so they have access to particular shared canvases
let specialCases : List<OwnerName.T * UserName.T> =
  [ (OwnerName.create "pixelkeet", UserName.create "laxels")
    (OwnerName.create "rootvc", UserName.create "adam")
    (OwnerName.create "rootvc", UserName.create "lee")
    (OwnerName.create "talkhiring", UserName.create "harris")
    (OwnerName.create "talkhiring", UserName.create "anson") ]


let specialCasePermission
  (username : UserName.T)
  (ownerName : OwnerName.T)
  : Option<Permission> =
  if List.any ((=) (ownerName, username)) specialCases then
    Some ReadWrite
  else
    None

// People should have access to the canvases under their name
let matchPermission
  (username : UserName.T)
  (ownerName : OwnerName.T)
  : Option<Permission> =
  if (username.ToString()) = (ownerName.ToString()) then Some ReadWrite else None

// Everyone should have read-access to 'sample'.
let samplePermission (owner : OwnerName.T) : Option<Permission> =
  if "sample" = (owner.ToString()) then Some Read else None

// What's the highest level of access a particular user has to a
// particular user's canvas
let permission
  (owner : OwnerName.T)
  (ownerID : UserID)
  (username : UserName.T)
  : Task<Option<Permission>> =
  let permFs : List<unit -> Task<Option<Permission>>> =
    [ (fun _ -> task { return matchPermission username owner })
      // FSTODO: remove specialCasePermission
      (fun _ -> task { return specialCasePermission username owner })
      (fun _ -> task { return samplePermission owner }) ]
  // FSTODO: missing two permissions here
  // Return the greatest `permission option` of a set of functions producing
  // `permission option`, lazily, so we don't hit the db unnecessarily
  List.fold
    (task { return None })
    (fun (p : Task<Option<Permission>>) (f : unit -> Task<Option<Permission>>) ->
      task {
        match! p with
        | Some ReadWrite -> return Some ReadWrite
        | Some older ->
            match! f () with
            | Some newer -> return Some(older.max (newer))
            | None -> return! p
        | None -> return! f ()
      })
    permFs


let canEditCanvas
  (canvas : CanvasName.T)
  (ownerName : OwnerName.T)
  (ownerID : UserID)
  (username : UserName.T)
  : Task<bool> =
  task {
    match! permission ownerName ownerID username with
    | Some Read -> return false
    | Some ReadWrite -> return true
    | None -> return false
  }

let canViewCanvas
  (canvas : CanvasName.T)
  (ownerName : OwnerName.T)
  (ownerID : UserID)
  (username : UserName.T)
  : Task<bool> =
  task {
    match! permission ownerName ownerID username with
    | Some Read -> return true
    | Some ReadWrite -> return true
    | None -> return false
  }
