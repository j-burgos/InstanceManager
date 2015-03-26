
open System

open InstanceManager
open InstanceManager.Softlayer
open InstanceManager.Aws

[<EntryPoint>]
let main argv =

    let aws = InstanceManager(Backend.Aws)
    let sl = InstanceManager(Backend.Softlayer)

    let slVm = 
        {
            startCpus = 1
            maxMemory = 1024
            hostname = "vmtest"
            domain = "beyondgames.io"
            datacenter = { name = "dal05" }
            hourlyBillingFlag = true
            localDiskFlag = true
            operatingSystemReferenceCode = "UBUNTU_LATEST"
        }
    let awsVm = 
        {
            imageId = "ami-8efdc7cb"
            keyName = "rafael-beyond"
            instanceType = "t1.micro"
        }

    
    //let slOpts = sl.Options()
    let awsOpts = aws.Options()

    //let slList = sl.List()
    let awsList = aws.List()

    //let slRes = sl.Create(1, SLIns slVm)
    //let awsRes = aws.Create(1, AmazonIns awsVm) 


    0 // return an integer exit code