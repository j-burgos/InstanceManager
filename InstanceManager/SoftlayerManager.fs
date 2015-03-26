
namespace InstanceManager.Softlayer

open System

open FSharp.Data
open FSharp.Data.JsonExtensions
open RestSharp

type Processor =
    {
        description : string
        hourlyFee : decimal
    }

type SoftlayerOptions =
    {
        processors: list<Processor>
        memory: list<Processor>
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
        modifyDate      : DateTime
        provisionDate   : DateTime

        status          : string

    }

type Datacenter = { name : string }

type VmParams = 
    {
        hostname            : string
        domain              : string
        datacenter          : Datacenter

        maxMemory           : int
        startCpus           : int

        hourlyBillingFlag   : bool
        localDiskFlag       : bool

        operatingSystemReferenceCode : string
    }

type CreationParams = { parameters : List<VmParams> }

type SoftlayerManager() = 

    let username        = ""
    let apikey          = ""

    let baseUrl         = "https://api.softlayer.com/rest/v3/"
    let format          = "json"

    let accountService  = "SoftLayer_Account/"
    let vmService       = "SoftLayer_Virtual_Guest/"

    let client          = RestClient(baseUrl)
    let authenticator   = HttpBasicAuthenticator(username,apikey)

    do client.Authenticator <- authenticator


    member this.DoRequest(req) =
        let response = client.Execute(req)
        let json = 
            match response.Content with
                | "true" -> JsonValue.Boolean(true)
                | _ -> JsonValue.Parse response.Content
        json

    member this.Options() = 
        let req = RestRequest(vmService + "getCreateObjectOptions.json", Method.GET)
        let json = this.DoRequest req
        let processors = 
            List.ofSeq(json?processors.AsArray()) 
            |> List.map (fun item -> 
                {
                    description = item?itemPrice?item?description.AsString()
                    hourlyFee = item?itemPrice?hourlyRecurringFee.AsDecimal()
                })
        let memoryOptions = 
            List.ofSeq(json?memory.AsArray()) 
            |> List.map (fun item -> 
                {
                    description = item?itemPrice?item?description.AsString()
                    hourlyFee = item?itemPrice?hourlyRecurringFee.AsDecimal()
                })
        {
            processors = processors
            memory = memoryOptions
        }

    member this.List() =
        let req = RestRequest(accountService + "getVirtualGuests.json", Method.GET)
        let json = this.DoRequest req
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
                modifyDate  = (vm?modifyDate.AsDateTime())
                provisionDate = (vm?createDate.AsDateTime())
                status      = (vm?status?name.AsString())
            })

    member this.Create(numberOfInstances:int, instance:VmParams) = 

        let req = RestRequest("SoftLayer_Virtual_Guest.json", Method.POST)
        let vm = { 
                startCpus = instance.startCpus
                maxMemory = instance.maxMemory
                hostname = instance.hostname
                domain = instance.domain
                datacenter = { name = instance.datacenter.name }
                hourlyBillingFlag = instance.hourlyBillingFlag
                localDiskFlag = instance.localDiskFlag
                operatingSystemReferenceCode = instance.operatingSystemReferenceCode
            }
        let vmList = [1 .. numberOfInstances] |> List.map (fun item -> vm)
        let reqParams = { parameters = vmList }

        let json = this.DoRequest <| req.AddJsonBody(reqParams)
        {
            id      = (json?id.AsInteger())
            cpus    = (json?startCpus.AsInteger())
            memory  = (json?maxMemory.AsInteger())

            hostname    = (json?hostname.AsString())
            domain      = (json?domain.AsString())
            fullHostname = (json?fullyQualifiedDomainName.AsString())

            publicIp = ""
            privateIp = ""

            createDate = (json?createDate.AsDateTime())
            modifyDate = (json?modifyDate.AsDateTime())
            provisionDate = (json?provisionDate.AsDateTime())

            status = ""
        }

    member this.Delete(instanceId:string) = 
        let req = RestRequest(vmService + instanceId.ToString() + ".json", Method.DELETE)
        let json = this.DoRequest req
        json