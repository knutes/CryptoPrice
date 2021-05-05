using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CryptoPrice
{
    public class Coin
    {
        public string Symbol { get; set; }
        public string Price { get; set; }
    }
    public class Function
    {
        private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();

        public static readonly HttpClient httpClient = new HttpClient();
        public async Task<ExpandoObject> FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
        {
            string url = "https://api.coincap.io/v2/assets/";

            string coin = "";
            Dictionary<string, string> dict = (Dictionary<string, string>)input.QueryStringParameters;
            dict.TryGetValue("coin", out coin);
            string call = url + coin;

            HttpResponseMessage response = await httpClient.GetAsync(call);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            dynamic expandoObject = JsonConvert.DeserializeObject<ExpandoObject>(json);

            Coin coinToAdd = new Coin();
            coinToAdd.Symbol = expandoObject.data.symbol;
            coinToAdd.Price = expandoObject.data.priceUsd;

            var request = new UpdateItemRequest
            {
                TableName = "CryptoCoins",
                Key = new Dictionary<string, AttributeValue>
                        {
                            { "symbol", new AttributeValue { S = coinToAdd.Symbol } }
                        },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>()
                        {
                            {
                                "price",
                                new AttributeValueUpdate { Action = "PUT", Value = new AttributeValue { N = coinToAdd.Price } }
                            }
                        },

            };
            await client.UpdateItemAsync(request);

            return expandoObject;
        }
    }
}
