﻿
namespace InstanceManager.Softlayer

open System
open System.IO

open FSharp.Data
open FSharp.Data.JsonExtensions
open RestSharp

type InstanceFeature =
    {
        value       : int
        description : string
        hourlyFee   : decimal
        recurringFee: decimal

    }

type OperatingSystem =
    {
        code        : string
        description : string
    }
type SshKey =
    {
        id : string
        label : string
        notes : string
    }
type Datacenter = 
    { 
        name : string 
    }

type SoftlayerOptions =
    {
        processors          : InstanceFeature list
        memory              : InstanceFeature list
        datacenters         : Datacenter list
        operatingSystems    : OperatingSystem list
        sshKeys             : SshKey list
    }

type SoftlayerInstance = 
    {
        id              : int
        cpus            : int
        memory          : int

        hostname        : string
        domain          : string
        fullHostname    : string

        publicIp        : string
        privateIp       : string

        createDate      : DateTime
        modifyDate      : DateTime option
        provisionDate   : DateTime option

        status          : string
    }

type Id ={
    id : string
}

type VmParams = 
    {
        startCpus           : int
        maxMemory           : int

        hostname            : string
        domain              : string
        datacenter          : Datacenter

        hourlyBillingFlag   : bool
        localDiskFlag       : bool

        sshKeys             : Id list

        operatingSystemReferenceCode : string
    }

type CreationParams = { parameters : VmParams list list }

type SoftlayerManager() = 

    let path = Path.Combine [| Environment.GetEnvironmentVariable "HOME"; ".softlayer" ; "credentials.json" |]
    do if not (File.Exists path) then failwith ("Expected credentials file at: " + path)

    let credentials = JsonValue.Load path
    let username = (credentials?username).AsString()
    let apiKey = (credentials?apiKey).AsString()

    let baseUrl         = "https://api.softlayer.com/rest/v3/"
    let format          = "json"

    let accountService  = "SoftLayer_Account/"
    let vmService       = "SoftLayer_Virtual_Guest/"

    let client          = RestClient(baseUrl)
    let authenticator   = HttpBasicAuthenticator(username,apiKey)

    do client.Authenticator <- authenticator


    member this.DoRequest(req) =
        let response = client.Execute(req)
        let json = 
            match response.Content with
            | "true" -> JsonValue.Boolean(true)
            | _ -> JsonValue.Parse response.Content
        json

    member this.Keys() = 
        let req = RestRequest(accountService + "getSshKeys.json", Method.GET)
        let json = this.DoRequest req
        let keys = List.ofSeq(json.AsArray()) 
        keys |> List.map (fun item -> 
            {
                id = item?id.AsString()
                label = item?label.AsString()
                notes = if item.TryGetProperty("notes") = None then "" else item?notes.AsString()
            })

    member this.Options() = 
        let req = RestRequest(vmService + "getCreateObjectOptions.json", Method.GET)
        let json = this.DoRequest req
        let processors = 
            List.ofSeq(json?processors.AsArray()) 
            |> List.map (fun item -> 
                {
                    value = item?template?startCpus.AsInteger()
                    description = item?itemPrice?item?description.AsString()
                    hourlyFee = item?itemPrice?hourlyRecurringFee.AsDecimal()
                    recurringFee = item?itemPrice?recurringFee.AsDecimal()
                })
        let memoryOptions = 
            List.ofSeq(json?memory.AsArray()) 
            |> List.map (fun item -> 
                {
                    value = item?template?maxMemory.AsInteger()
                    description = item?itemPrice?item?description.AsString()
                    hourlyFee = item?itemPrice?hourlyRecurringFee.AsDecimal()
                    recurringFee = item?itemPrice?recurringFee.AsDecimal()
                })

        let operatingSystems = 
            List.ofSeq(json?operatingSystems.AsArray()) 
            |> List.map (fun item -> 
                {
                    description = item?itemPrice?item?description.AsString()
                    code = item?template?operatingSystemReferenceCode.AsString()
                })

        let datacenters = 
            List.ofSeq(json?datacenters.AsArray()) 
            |> List.map (fun item -> 
                {
                    name = item?template?datacenter?name.AsString()
                })

        let keys = this.Keys()
        {
            processors          = processors
            memory              = memoryOptions
            operatingSystems    = operatingSystems
            datacenters         = datacenters
            sshKeys             = keys
        }

    member this.GetInstance(instanceId) = 
        let req = RestRequest(vmService + instanceId.ToString() + ".json", Method.GET)
        let ins = this.DoRequest <| req.AddQueryParameter("objectMask", "status;datacenter")
        {
                id      = (ins?id.AsInteger())
                cpus    = (ins?startCpus.AsInteger())
                memory  = (ins?maxMemory.AsInteger())

                hostname    = (ins?hostname.AsString())
                domain      = (ins?domain.AsString())
                fullHostname = (ins?fullyQualifiedDomainName.AsString())

                publicIp    = if ins.TryGetProperty("primaryIpAddress") = None then "" else (ins?primaryIpAddress.AsString())
                privateIp   = if ins.TryGetProperty("primaryBackendIpAddress") = None then "" else (ins?primaryBackendIpAddress.AsString())

                createDate = DateTime()//if ins.TryGetProperty("createDate") = None then DateTime() else (ins?createDate.AsDateTime())
                modifyDate = Some(DateTime()) //if ins.TryGetProperty("modifyDate") = None then None else Some (ins?modifyDate.AsDateTime())
                provisionDate = Some(DateTime())//if ins.TryGetProperty("provisionDate") = None then None else Some (ins?provisionDate.AsDateTime())

                status = (ins?status?name.AsString())
            }

    member this.List() =
        let req = RestRequest(accountService + "getVirtualGuests.json",Method.GET)
        let json = this.DoRequest <| req.AddQueryParameter("objectMask", "status;datacenter")
        let jsonVms = json.AsArray()
        let list = List.ofArray(jsonVms)
        list |> List.map ( fun vm -> 
            {
                id          = (vm?id.AsInteger())
                cpus        = (vm?startCpus.AsInteger())
                memory      = (vm?maxMemory.AsInteger())
                hostname    = (vm?hostname.AsString())
                domain      = (vm?domain.AsString())
                fullHostname= (vm?fullyQualifiedDomainName.AsString())
                publicIp    = (vm?primaryIpAddress.AsString())
                privateIp   = (vm?primaryBackendIpAddress.AsString())
                createDate  = (vm?createDate.AsDateTime())
                modifyDate  = Some (vm?modifyDate.AsDateTime())
                provisionDate = Some (vm?createDate.AsDateTime())
                status      = (vm?status?name.AsString())
            })

    member this.Create(numberOfInstances, instance) = 
        let keys = this.Keys()
        let key = 
            keys
            |> List.filter (fun item -> item.label = instance.sshKeys.Head.id)
            |> List.head 
        let req = RestRequest(vmService + "createObjects.json", Method.POST)
        let vmList = 
            [1 .. numberOfInstances] 
            |> List.map (fun i ->
                let hostname = 
                    if numberOfInstances > 1 then 
                        instance.hostname + (sprintf "%04d" (i-1)) else 
                        instance.hostname
                { 
                    startCpus = instance.startCpus
                    maxMemory = instance.maxMemory
                    hostname = hostname
                    domain = instance.domain
                    datacenter = { name = instance.datacenter.name }
                    hourlyBillingFlag = instance.hourlyBillingFlag
                    localDiskFlag = instance.localDiskFlag
                    operatingSystemReferenceCode = instance.operatingSystemReferenceCode
                    sshKeys = [{ id = key.id }]
                })

        let reqParams = { parameters = [vmList] }

        let json = this.DoRequest <| req.AddJsonBody(reqParams)
        let instances = Seq.toList <| json.AsArray()
        instances 
        |> List.map (fun ins -> 
            {
                id      = (ins?id.AsInteger())
                cpus    = (ins?startCpus.AsInteger())
                memory  = (ins?maxMemory.AsInteger())

                hostname    = (ins?hostname.AsString())
                domain      = (ins?domain.AsString())
                fullHostname = (ins?fullyQualifiedDomainName.AsString())

                publicIp = ""
                privateIp = ""

                createDate = (ins?createDate.AsDateTime())
                modifyDate = None
                provisionDate = None

                status = ""
            })

    member this.Delete(instanceId) = 
        let req = RestRequest(vmService + instanceId.ToString() + ".json", Method.DELETE)
        let json = this.DoRequest req
        json