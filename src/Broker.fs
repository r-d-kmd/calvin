//This is a temporary build file and should not be altered
//If changes are need edit common/hobbes.messaging/src/Broker.fs
namespace Hobbes.Messaging

open RabbitMQ.Client
open RabbitMQ.Client.Events
open System
open System.Text

open Newtonsoft.Json

module Broker = 
    let inline private env name defaultValue = 
        match System.Environment.GetEnvironmentVariable name with
        null -> defaultValue
        | v -> v.Trim()
    let inline hash (input : string) =
        use md5Hash = System.Security.Cryptography.MD5.Create()
        let data = md5Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input))
        let sBuilder = StringBuilder()
        (data
        |> Seq.fold(fun (sBuilder : System.Text.StringBuilder) d ->
                sBuilder.Append(d.ToString("x2"))
        ) sBuilder).ToString()  
    let private serialize<'a> (o:'a) = JsonConvert.SerializeObject(o)
    let private deserialize<'a> json = JsonConvert.DeserializeObject<'a>(json)

    [<RequireQualifiedAccess>]
    type Queue = 
        CacheQueue
        | AzureDevOpsQueue
        | GitQueue
        | CalculationQueue
        | DeadLetterQueue
        | LogQueue
        | GenericQueue of string
        with member x.Name
               with get() = 
                    match x with
                    CacheQueue -> "cache"
                    | AzureDevOpsQueue -> "azuredevops"
                    | GitQueue -> "git"
                    | CalculationQueue -> "calculation"
                    | DeadLetterQueue -> "deadletter"
                    | LogQueue -> "log"
                    | GenericQueue name -> name.ToLower()

    type CacheMessage = 
        Updated of string
        | Empty

    type SyncMessage = 
        Sync of string
        | Empty

    type DeadLetter =
        {
            OriginalQueue : Queue
            OriginalMessage : string
            ExceptionMessage : string
            ExceptionStackTrace : string
        }

    type TransformationMessageBody = 
        {
            Name : string
            Statements : seq<string>
        }

    type LogMessage = 
        MessageCompletion of json:string
        | MessageFailure of msg:string * json:string
        | MessageException of msg:string * json:string

    type TransformMessage = 
        {
            Transformation : TransformationMessageBody
            DependsOn : string
        }

    type MergeMessage =
        {
            CacheKey : string
            Datasets : string []
        }
     
    type JoinMessage = 
        {
            CacheKey : string
            Left : string
            Right : string
            Field : string
        }

    type Format = 
        Json

    type FormatMessage = 
        {
            CacheKey : string
            Format : Format
        }

    type CalculationMessage = 
        Transform of TransformMessage
        | Merge of MergeMessage
        | Join of JoinMessage
        | Format of FormatMessage

    type Message<'a> = 
        | Message of 'a

   
        

    let private user = 
        match env "RABBIT_USER" null with
        null -> failwith "'USER' not configured"
        | u -> u
    let private password =
        match env "RABBIT_PASSWORD" null with
        null -> failwith "'PASSWORD' not configured"
        | p -> p
    let private host = 
        match env "RABBIT_HOST" null with
        null -> failwith "Queue 'HOST' not configured"
        | h -> h
    let private port = 
        match env "RABBIT_PORT" null with
        null -> failwith "Post not specified"
        | p -> int p
    let private watchDogInterval = 
        env "WATCH_DOG_INTERVAL" "1" |> int
    
    let private init() =
        let factory = ConnectionFactory()
        
        factory.HostName <- host
        factory.Port <- port
        factory.UserName <- user
        factory.Password <- password
        let connection = factory.CreateConnection()
        let channel = connection.CreateModel()
        //to limit memory pressure, we're only going to handle one message at a time in any consumer
        //channel.BasicQos(1u,0us,true)
        channel
    

    let private declare (channel : IModel) (queue : Queue) =  
        channel.QueueDeclare(queue.Name,
                                 true,
                                 false,
                                 false,
                                 null) |> ignore
            
    let awaitQueue() =
        let rec inner tries = 
            let waitms = 5000
            let retry() =  
                async{
                    do! Async.Sleep waitms
                    return! inner (tries + 1)
                }
            async{
                try
                    let channel = init()
                    declare channel Queue.DeadLetterQueue
                with e -> 
                    if tries % (60000 / waitms) = 0 then //write the message once every minute
                        printfn "Queue not yet ready. Message: %s" e.Message
                    do! retry()
            } 
        inner 0

    type MessageResult = 
        Success
        | Failure of string
        | Excep of Exception

    let private publishString queue (message : string) =
        try
            let channel = init()
            declare channel queue
            
            let body = ReadOnlyMemory<byte>(Text.Encoding.UTF8.GetBytes(message))
            let properties = channel.CreateBasicProperties()
            properties.Persistent <- true

            channel.BasicPublish("",queue.Name, true,properties,body)
           
        with e -> 
           eprintfn "Failed to publish to the queue. Message: %s" e.Message

    let private publish<'a> queueName (message : Message<'a>) =    
        message
        |> serialize
        |> publishString queueName
    
         
    let private watch<'a> queue (handler : 'a -> MessageResult) shouldLog =
        let mutable keepAlive = true
        let fails = Collections.Concurrent.ConcurrentDictionary<uint64,int>()
        try
            let channel = init()
            declare channel queue
            let logAndComplete ack f json = 
                ack()
                if shouldLog then
                    f json
                    |> Message
                    |> publish Queue.LogQueue

            
            let consumer = EventingBasicConsumer(channel)
            let messageException tag (e : Exception) json = 
                logAndComplete (fun () -> channel.BasicReject(tag,false)) (fun l -> MessageException(e.Message, l)) json
                {
                        OriginalQueue = queue
                        OriginalMessage = json
                        ExceptionMessage = e.Message
                        ExceptionStackTrace = e.StackTrace
                } |> Message 
                |> publish Queue.DeadLetterQueue

            consumer.Received.AddHandler(EventHandler<BasicDeliverEventArgs>(fun _ (ea : BasicDeliverEventArgs) -> 
                    let msgText = 
                        Encoding.UTF8.GetString(ea.Body.ToArray())
                    try        
                        let msg = 
                            msgText
                            |> deserialize<Message<'a>>

                        let tag = ea.DeliveryTag
                        match msg with
                        | Message msg ->
                            match msg |> handler with
                            Success ->
                                let m = sprintf "%A" msg
                                printfn "Message processed successfully. %s" (m.Substring(0,min 100 m.Length))
                                logAndComplete (fun () -> channel.BasicAck(tag,false)) MessageCompletion (serialize msg)
                            | Failure m ->
                                let json = (serialize msg)
                                let m = 
                                    sprintf "Message could not be processed (%s). %s" m json
                                printfn "%s" m
                                logAndComplete (fun () -> 
                                       let failCount = fails.AddOrUpdate(tag,1,fun a b -> fails.[a] + b)
                                       channel.BasicReject(tag,failCount < 5)) (fun l -> MessageFailure(json, m)
                                    ) (serialize msg)
                            | Excep e ->
                                messageException ea.DeliveryTag e (serialize msg)
                    with e ->
                        messageException ea.DeliveryTag e msgText
                        eprintfn "Failed to parse message (%s) (Message will be ack'ed). Error: %s %s" msgText e.Message e.StackTrace 
                )
            )
            
            channel.BasicConsume(queue.Name,false,consumer) |> ignore
            printfn "Watching queue: %s" queue.Name
            
            while keepAlive do
                System.Threading.Thread.Sleep(watchDogInterval / 2)
         with e ->
           eprintfn "Failed to subscribe to the queue. %s:%d. Message: %s" host port e.Message
           keepAlive <- false
    
    type Broker() =
        do
            let channel = init()
            declare channel Queue.DeadLetterQueue
        static member MessageCount (queue : Queue) =
            init().MessageCount queue.Name
        static member Cache(msg : CacheMessage) = 
            publish Queue.CacheQueue (Message msg)
        static member Cache (handler : CacheMessage -> _) = 
            watch Queue.CacheQueue handler true
        static member AzureDevOps(msg : SyncMessage) = 
            publish Queue.AzureDevOpsQueue (Message msg)
        static member AzureDevOps (handler : SyncMessage -> _) = 
            watch Queue.AzureDevOpsQueue handler true
        static member Git(msg : SyncMessage) = 
            publish Queue.GitQueue (Message msg)
        static member Git (handler : SyncMessage -> _) = 
            watch Queue.GitQueue handler true
        static member Calculation(msg : CalculationMessage) = 
            publish Queue.CalculationQueue (Message msg)
        static member Calculation (handler : CalculationMessage -> _) = 
            watch Queue.CalculationQueue handler true 
        static member Log(msg : LogMessage) = 
            publish Queue.LogQueue (Message msg)
        static member Log (handler : LogMessage -> _) = 
            watch Queue.LogQueue handler false
        static member Generic queueName msg =
            assert(queueName |> String.IsNullOrWhiteSpace |> not)
            publishString (Queue.GenericQueue queueName) msg
