
open System
open System.Threading

open Mono.Options

open InstanceManager

open InstanceManager.Aws
open InstanceManager.Softlayer

open InstanceManagement

let printOptions options = 
    match options with 
        | SLOptions opts ->
            printfn "= Cpus ="
            for p in opts.processors do
                printfn " %d - %s | $%M/hr - $%M/mth" p.value p.description p.hourlyFee p.recurringFee
            printfn ""

            printfn "= Memory ="
            for m in opts.memory do
                printfn " %d - %s | $%M/hr - $%M/mth" m.value m.description m.hourlyFee m.recurringFee
            printfn ""

            printfn "= Operating systems ="
            for os in opts.operatingSystems do
                printfn " %s - %s" os.code os.description
            printfn ""

            printfn "= Datacenters ="
            for dc in opts.datacenters do
                printfn " %s" dc.name
            printfn ""

        | AmazonOptions opts ->
            printfn "= Images ="
            for img in opts.images do
                printfn " %s - %s" img.imageId img.imageName
            printfn ""

let printInstances instances = 
    for instance in instances do
        match instance with
        | SLInstance ins -> 
            printfn "[%s] %s" ins.fullHostname ins.status
            printfn " Id: %d" ins.id
            printfn " Created at: %A" ins.createDate
            printfn " Cpus: %d - Memory: %dGB" ins.cpus (ins.memory/1024)
            printfn " PublicIP: %s - PrivateIP: %s" ins.publicIp ins.privateIp
            printfn ""
        | AmazonInstance ai -> 
            printfn "[%s] %s" ai.name ai.status
            printfn " Id: %s - ImageId: %s" ai.id ai.imageId
            printfn " Created at: %A" ai.launchTime
            printfn " Type: %s" ai.instanceType
            printfn " KeyName: %s" ai.keyName
            printfn ""

[<EntryPoint>]
let main argv =

    //let optionSet = OptionParser
    let aws = getManager(Backend.Aws)
    let sl = getManager(Backend.Softlayer)

    let slOpts = getOptions sl
    let awsOpts = getOptions aws
    //printOptions slOpts
    //printOptions awsOpts


    let slVm = 
        {
            startCpus = 1
            maxMemory = 1024
            hostname = "vmprovisiontest"
            domain = "beyondgames.io"
            datacenter = { name = "dal05" }
            hourlyBillingFlag = true
            localDiskFlag = true
            operatingSystemReferenceCode = "DEBIAN_LATEST"
        }
    let awsVm = 
        {
            imageId = "ami-8efdc7cb"
            keyName = "rafael-beyond"
            instanceType = "t1.micro"
        }


    //let slCreate = createInstances(sl, 1, SLIns(slVm))
    //let awsCreate = createInstances(aws, 2, AmazonIns(awsVm))

    //Thread.Sleep 5000

    let slInstances = getInstances sl
    let awsInstances = getInstances aws
    printInstances slInstances 
    printInstances awsInstances 
    //let slRes = createInstances(sl, 1, SLIns(slVm))



    0 // return an integer exit code