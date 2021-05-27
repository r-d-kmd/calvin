namespace FSharp.Data.GraphQL.Samples.StarWarsApi

module HTMLFiles =
    let burndown = """
    <html>
        <head>
            <script src="https://d3js.org/d3.v4.js"></script>
            <style>
                html,body{
                    margin: 0px;
                    padding: 0px;
                    background-color: #3876b7;
                }
                #chart {
                    display:flex;
                    justify-content:center;
                    align-items:center;
                    height: 100vh;
                }
                text, span {
                    font-family: Verdana,sans-serif;
                    fill: white;
                    color: white;
                }
            </style>
            <script>
                window.onload=function() {
                    const margin = {top: 20, right: 30, bottom: 100, left: 120},
                        width = 800 - margin.left - margin.right,
                        height = 400 - margin.top - margin.bottom;
                    const axisSpacing = 30;
                    const q = `# Write your query or mutation here
                                {
                                GetSprintLayers{
                                    Projects{
                                    Sprints{
                                        SprintNumber
                                        WorkItems{
                                        SprintName
                                        SprintNumber
                                        CreatedDate
                                        ClosedDate
                                        WorkItemID
                                        }
                                    }
                                    }
                                }
                                }`;
                    async function postData(url = '', data = {}) {
                        const response = await fetch(url, {
                            method: 'POST', // *GET, POST, PUT, DELETE, etc.
                            mode: 'cors', // no-cors, *cors, same-origin
                            cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
                            credentials: 'same-origin', // include, *same-origin, omit
                            headers: {
                            'Content-Type': 'application/json'
                            // 'Content-Type': 'application/x-www-form-urlencoded',
                            },
                            redirect: 'follow', // manual, *follow, error
                            referrerPolicy: 'no-referrer', // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
                            body: JSON.stringify(data) // body data type must match "Content-Type" header
                        });
                        return response.json(); // parses JSON response into native JavaScript objects
                    }
        
                    postData('/graphql', {"operationName":null,"variables":{},"query":q})
                    .then(response => {
                        const sprintData = response.data.getSprintLayers[0].projects[0].sprints;
                        const closedWorkItems = sprintData
                            .reduce((acc, e) => [ ...acc, ...e.workItems ], [])
                            .map(e => ({
                                ...e,
                                closedDate: e.closedDate ? Date.parse(e.closedDate) : 0,
                                createdDate: Date.parse(e.createdDate),
                            }))
                            .filter(e => e.closedDate !== 0 || e.workItemID.startsWith("[ACTION]"))
                        const sprints = closedWorkItems
                            .reduce((acc, e) => {
                                if(acc[e.sprintNumber]) {
                                    acc[e.sprintNumber].value += 1;
                                } else {
                                    acc[e.sprintNumber] = { value: 1, sprintNumber: e.sprintNumber };
                                }
                                return acc;
                            }, {});
                        const sprintList = Object
                            .values(sprints)
                            .reduce((acc, sprintNumber) => {acc.push(sprintNumber); return acc;}, [])
                            .sort((a, b) => (a.sprintNumber) > (b.sprintNumber) ? 1 : -1)
                        //taskList.map(e => con sole.log(`${e.sprintNumber} ${e.workItemID} ${new Date(e.closedDate)}`))
        
                        /*
                        [2, 2, 2]
                        [6, 4, 2]
                        */
                        const burnDown = sprintList
                            .map(function(e, i) {
                                let sum = 0;
                                for (let j = i; j < sprintList.length; j++) {
                                    const element = sprintList[j];
                                    sum += element.value;
                                }
                                return {
                                    value: sum,
                                    sprintNumber: e.sprintNumber - 1,
                                };
                            })
                            //.sort((a, b) => (a.sprintNumber) > (b.sprintNumber) ? 1 : -1);
                        burnDown.push({ value: 0, sprintNumber: burnDown.length });
                        const data = burnDown;
        
                        d3.select("#status")
                            .attr("hidden", "hidden");
        
                        const svg = d3.select("#chart")
                            .append("svg")
                                .attr("width", width + margin.left + margin.right)
                                .attr("height", height + margin.top + margin.bottom)
                            .append("g")
                                .attr("transform",
                                    "translate(" + margin.left + "," + margin.top + ")");
                        // Add X axis
                        var x = d3.scaleLinear()
                          .domain(d3.extent(data, function(d) { return d.sprintNumber; }))
                          .range([ 0, width ]);
                        svg.append("g")
                          .attr("transform", "translate(0," + (height + axisSpacing) + ")")
                          .call(
                            d3.axisBottom(x)
                              .ticks(burnDown.length)
                              .tickSize(-height)
                          );
        
                        // Add Y axis
                        var y = d3.scaleLinear()
                          .domain([0, d3.max(data, function(d) { return +d.value; })])
                          .range([ height, 0 ]);
                        svg.append("g")
                          .attr("transform", "translate("+ (-axisSpacing) + ",0)")
                          .call(
                            d3.axisLeft(y)
                              .tickSize(-width*1.1)
                          );
        
                        // Style tick-lines to work as a grid
                        svg.selectAll(".tick line").attr("stroke", "#67a4e3")
                        svg.selectAll(".tick text").attr("font-size", 18)
        
                        // Not sure why this suddenly shows up
                        svg.selectAll(".domain").attr("display", "none")
        
                        svg.append("text")
                          .attr("text-anchor", "middle")
                          .attr("x", width/2)
                          .attr("y", height + margin.bottom/2 + axisSpacing)
                          .text("Sprint");
        
                        svg.append("text")
                          .attr("text-anchor", "middle")
                          .attr("x", width/2)
                          .attr("y", 20)
                          .attr("font-size", 32)
                          .text("Workitems completed");
        
                        // Add the line
                        svg.append("path")
                          .datum(data)
                          .attr("fill", "none")
                          .attr("stroke", "white")
                          .attr("stroke-width", 6)
                          .attr("d", d3.line()
                              .x(function(d) { return x(d.sprintNumber) })
                              .y(function(d) { return y(d.value) })
                          )
                        // Add dots
                        svg.append('g')
                          .selectAll("dot")
                          .data(data)
                          .enter()
                          .append("circle")
                            .attr("cx", function (d) { return x(d.sprintNumber); } )
                            .attr("cy", function (d) { return y(d.value); } )
                            .attr("r", 16)
                            .attr("stroke", "white")
                            .attr("stroke-width", 4)
                            .style("fill", function (d) { return "#3876b7" } )
                                  });
                              }
            </script>
        </head>
        <body>
            <div id="chart"><span id="status">Loading...</span></div>
        </body>
        </html>
    """

    let playground = """
        <!DOCTYPE html>
        <html>
        <!--
            License: MIT
            Source: https://github.com/graphql/graphql-playground/blob/master/packages/graphql-playground-html/minimal.html
        -->
        <head>
        <meta charset=utf-8/>
        <meta name="viewport" content="user-scalable=no, initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0, minimal-ui">
        <title>GraphQL Playground</title>
        <link rel="stylesheet" href="//cdn.jsdelivr.net/npm/graphql-playground-react/build/static/css/index.css" />
        <link rel="shortcut icon" href="//cdn.jsdelivr.net/npm/graphql-playground-react/build/favicon.png" />
        <script src="//cdn.jsdelivr.net/npm/graphql-playground-react/build/static/js/middleware.js"></script>
        </head>

        <body>
        <div id="root">
            <style>
            body {
                background-color: rgb(23, 42, 58);
                font-family: Open Sans, sans-serif;
                height: 90vh;
            }

            #root {
                height: 100%;
                width: 100%;
                display: flex;
                align-items: center;
                justify-content: center;
            }

            .loading {
                font-size: 32px;
                font-weight: 200;
                color: rgba(255, 255, 255, .6);
                margin-left: 20px;
            }

            img {
                width: 78px;
                height: 78px;
            }

            .title {
                font-weight: 400;
            }
            </style>
            <img src='//cdn.jsdelivr.net/npm/graphql-playground-react/build/logo.png' alt=''>
            <div class="loading"> Loading
            <span class="title">GraphQL Playground</span>
            </div>
        </div>
        <script>window.addEventListener('load', function (event) {
            GraphQLPlayground.init(document.getElementById('root'), {
                // options as 'endpoint' belong here
                shareEnabled: true,
                settings: {
                    'request.credentials': 'same-origin',
                },
            })
            })</script>
        </body>

        </html>
    """