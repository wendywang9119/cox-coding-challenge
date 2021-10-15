using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IO.Swagger.Api;
using IO.Swagger.Model;

namespace MainApp
{
    class Program
    {
        static void Main(string[] args)
        {
            AnswerResponse answerResponse = GetAnswerResponse().Result;
            Console.WriteLine("Retrieving the data...");
            Console.WriteLine(answerResponse);
            Console.ReadKey();
        }

        static readonly string basePath = "http://api.coxauto-interview.com";

        static async Task<AnswerResponse> GetAnswerResponse()
        {
            // get dataSetId
            DataSetApi dataSetApi = new DataSetApi(basePath);
            string dataSetId = dataSetApi.GetDataSetId().DatasetId;

            // get all vehicleIds by dataSetId
            VehiclesApi vehiclesApi = new VehiclesApi(basePath);
            var vehicleIds = vehiclesApi.GetIds(dataSetId).VehicleIds;

            var stopwatch = Stopwatch.StartNew();

            List<Task<VehicleResponse>> vehicleTasks = new List<Task<VehicleResponse>>();
            // create a list of tasks so we can call vehicle api at same time
            foreach (var vehicleId in vehicleIds)
            {
                vehicleTasks.Add(vehiclesApi.GetVehicleAsync(dataSetId, vehicleId));
            }

            List<Task<DealersResponse>> dealerTasks = new List<Task<DealersResponse>>();
            Dictionary<int?, List<VehicleAnswer>> dict = new Dictionary<int?, List<VehicleAnswer>>(); // key = dealerId, value = listOfVehicles
            while (vehicleTasks.Any())
            {
                Task<VehicleResponse> finishedTask = await Task.WhenAny(vehicleTasks);
                vehicleTasks.Remove(finishedTask);
                VehicleResponse response = await finishedTask;

                if (!dict.ContainsKey(response?.DealerId))
                {
                    DealersApi dealersApi = new DealersApi(basePath);
                    dealerTasks.Add(dealersApi.GetDealerAsync(dataSetId, response.DealerId));
                    dict.Add(response.DealerId, new List<VehicleAnswer>());
                }
                
                dict[response.DealerId].Add(new VehicleAnswer(response.VehicleId, response.Year, response.Make, response.Model));
            }

            await Task.WhenAll(dealerTasks);
           
            List<DealerAnswer> answers = new List<DealerAnswer>();
            foreach(var task in dealerTasks)
            {
                var dealer = await task;
                answers.Add(new DealerAnswer()
                {
                    DealerId = dealer.DealerId,
                    Name = dealer.Name,
                    Vehicles = dict[dealer.DealerId]
                });
            }

            stopwatch.Stop();

            // post to anwser API
            Answer answer = new Answer(answers);
            AnswerResponse answerResponse = dataSetApi.PostAnswer(dataSetId, answer);
            return answerResponse;
        }
    }


}
