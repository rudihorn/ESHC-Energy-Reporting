module EnergyReporting.Helpers.Ldap

    open Microsoft.Extensions.Configuration
    open Novell.Directory.Ldap
    open System.Text.RegularExpressions

    type config = IConfiguration

    type LdapUser = {
        user : string;
        name : string;
        email : string;
        room : string;
    }

    (*
    member private this.GetLdapUsers () =
        let attr (entry : SearchResultEntry) name = 
            let prop = this.Configuration.[sprintf "LDAP:Properties:%s" name]
            let attr = entry.Attributes.[prop]
            if attr <> null && attr.Count > 0 then
                match attr.[0] with
                | :? string as v -> Some v
                | _ -> None
            else 
                None

        let server = this.Configuration.["LDAP:Host"]
        let cred = new NetworkCredential(this.Configuration.["LDAP:Bind:User"], this.Configuration.["LDAP:Bind:Pass"])
        let searchBase = this.Configuration.["LDAP:SearchBase"]
        let searchQuery = this.Configuration.["LDAP:SearchQuery"]
        let conn = new LdapConnection(server)
        conn.Credential <- cred
        conn.AuthType <- AuthType.Basic
        conn.SessionOptions.ProtocolVersion <- 3
        let searcher = new SearchRequest(searchBase, searchQuery, SearchScope.Subtree, null)
        let result = conn.SendRequest(searcher, TimeSpan.FromMinutes(1.0)) :?> SearchResponse
        let entries = 
            [for r in result.Entries do
                yield attr r "User", attr r "Name", attr r "Email", attr r "Room" ]
        let entries = entries |> List.choose (fun f -> 
            match f with 
            | Some u, Some n, Some e, Some r -> Some ({user = u; name = n; email = e; room = r}) 
            | _ -> None)
        entries
    *)

    (*take a flat and disect it into its individual components *)
    let normalizeFlat flat =
        let rm = Regex.Match(flat, "^(?<bld>[\\d]{2})/(?<flat>[\\d]{1,2})(?<room>[A-E])$")
        if rm.Success then
            let dat = int rm.Groups.["bld"].Value, int rm.Groups.["flat"].Value, rm.Groups.["room"].Value
            Some dat
        else 
            None

    let serializeFlat (bld, flat, room) = 
        sprintf "%02d/%02d" bld flat

    let users (config : config) =
        let attr (entry : LdapAttributeSet) name = 
            let prop = config.[sprintf "LDAP:Properties:%s" name]
            let attr = entry.getAttribute(prop)
            if attr <> null then
                Some attr.StringValue
            else 
                None

        let conn = new LdapConnection()
        conn.Connect(config.["LDAP:Host"], 389)
        conn.Bind(config.["LDAP:Bind:User"], config.["LDAP:Bind:Pass"])
        let searchQuery = config.["LDAP:SearchQuery"]
        let searchBase = config.["LDAP:SearchBase"]
        let search = conn.Search(searchBase, LdapConnection.SCOPE_SUB, searchQuery, null, false)
        let entries = 
          [
            while search.hasMore() do
              let entry = search.next()
              let r = entry.getAttributeSet()
              yield attr r "User", attr r "Name", attr r "Email", attr r "Room"
          ]
        let entries = entries |> List.choose (fun f -> 
            match f with 
            | Some u, Some n, Some e, Some r -> Some ({user = u; name = n; email = e; room = r}) 
            | _ -> None)
        entries