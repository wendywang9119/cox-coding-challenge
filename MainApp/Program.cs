using System;
using System.Collections.Generic;
using System.Linq;
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
            AnswerResponse answerResponse = GetAnswerResponse();
            Console.WriteLine("Retrieving the data...");
            Console.WriteLine(answerResponse);
            Console.ReadKey();
        }

        private static AnswerResponse GetAnswerResponse()
        {
            string basePath = "http://api.coxauto-interview.com";

            // get dataSetId
            DataSetApi dataSetApi = new DataSetApi(basePath);
            string dataSetId = dataSetApi.GetDataSetId().DatasetId;

            // get all vehicleIds by dataSetId
            VehiclesApi vehiclesApi = new VehiclesApi(basePath);
            List<int?> vehicleIds = vehiclesApi.GetIds(dataSetId).VehicleIds;

            Dictionary<int?, DealerAnswer> dic = new Dictionary<int?, DealerAnswer>();
            // iterate the list of vehicleIds
            foreach (var vehicleId in vehicleIds)
            {
                // get vehicle
                VehicleResponse vehicleResponse = vehiclesApi.GetVehicle(dataSetId, vehicleId);
                int? dealerId = vehicleResponse.DealerId;
                // when dictionary doesn't have the key
                if (!dic.ContainsKey(dealerId))
                {
                    List<VehicleAnswer> vehicleAnswers = new List<VehicleAnswer>();
                    vehicleAnswers.Add(new VehicleAnswer(vehicleResponse.VehicleId, vehicleResponse.Year, vehicleResponse.Make, vehicleResponse.Model));
                    // get dealer
                    DealersApi dealersApi = new DealersApi(basePath);
                    DealersResponse dealersResponse = dealersApi.GetDealer(dataSetId, dealerId);
                    DealerAnswer dealerAnswer = new DealerAnswer(dealersResponse.DealerId, dealersResponse.Name, vehicleAnswers);
                    dic.Add(dealerId, dealerAnswer);
                }
                else
                {
                    // get value of current key
                    List<VehicleAnswer> existingVehicleList = dic[dealerId].Vehicles;
                    existingVehicleList.Add(new VehicleAnswer(vehicleResponse.VehicleId, vehicleResponse.Year, vehicleResponse.Make, vehicleResponse.Model));
                }
            }

            // post to anwser API
            List<DealerAnswer> dealerAnswers = new List<DealerAnswer>();
            foreach(KeyValuePair<int?, DealerAnswer> entry in dic)
            {
                dealerAnswers.Add(dic[entry.Key]);
            }
            Answer answer = new Answer(dealerAnswers);

            AnswerResponse answerResponse = dataSetApi.PostAnswer(dataSetId, answer);
            return answerResponse;
        }
    }


}
