namespace sletmig

open FSharp.Data


module data =

   type Data = FSharp.Data.JsonProvider<"""[{
       "Project Name" : "lkjlkj",
       "TimeStamp": "03/11/2021 10:34:15",
       "Sprint Name": null,
       "WorkItemId": 79312,
       "ChangedDate": "04/30/2019 14:57:50",
       "WorkItemType": "User Story",
       "CreatedDate": "02/27/2019 12:27:11",
       "ClosedDate": "04/30/2019 14:57:50",
       "LeadTimeDays": "62.104618",
       "CycleTimeDays": "27.2778819",
       "StoryPoints": null,
       "RevisedDate": "01/01/9999 00:00:00",
       "Priority": 2,
       "Title": "Cake celebration at this point",
       "Sprint Number": null,
       "State": "Done"
     },{
       "Project Name" : "lkjlkj",
       "TimeStamp": "03/11/2021 10:34:15",
       "Sprint Name": "Iteration 3",
       "WorkItemId": 77520,
       "ChangedDate": "03/27/2019 14:42:54",
       "WorkItemType": "User Story",
       "CreatedDate": "02/13/2019 08:31:15",
       "ClosedDate": "03/27/2019 14:42:54",
       "LeadTimeDays": "42.2580902",
       "CycleTimeDays": "0.0",
       "StoryPoints": 3,
       "RevisedDate": "01/01/9999 00:00:00",
       "Priority": 2,
       "Title": "user specific dashboards",
       "Sprint Number": 3,
       "State": "Done"
     }]""">

   let data = 
      Http.RequestString("",
                          httpMethod = "get",
                          headers = [HttpRequestHeaders.BasicAuth "bruger" "pwd"]
      ) |> Data.Load

   let sprintProjectHeirachy = 
       data
       |> Array.groupBy(fun d -> d.SprintNumber)
       |> Seq.map(fun (_,records) ->
          records
          |> Seq.groupBy(fun r -> r.ProjectName)
       )