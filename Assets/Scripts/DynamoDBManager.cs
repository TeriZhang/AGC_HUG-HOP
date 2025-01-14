using UnityEngine;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DynamoDBManager : MonoBehaviour
{
    private static AmazonDynamoDBClient client;
    private const string TableName = "GameLeaderboard";
    private const string Region = "us-east-2"; // Change to your region
    private bool isInitialized = false;

    private async void Start()
    {
        await InitializeAWSClient();
    }

    private async Task InitializeAWSClient()
    {
        try
        {
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(Region)
            };

            client = new AmazonDynamoDBClient(AWSManager.Instance.Credentials, config);
            isInitialized = true;
            Debug.Log($"DynamoDB client initialized successfully in region {Region}");

            await TestConnection();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize DynamoDB client: {e.Message}");
            isInitialized = false;
        }
    }

    private async Task TestConnection()
    {
        try
        {
            Debug.Log($"Testing connection to DynamoDB table '{TableName}' in region '{Region}'...");
            
            var request = new DescribeTableRequest
            {
                TableName = TableName
            };
            
            var response = await client.DescribeTableAsync(request);
            Debug.Log($"Successfully connected to DynamoDB table: {TableName}");
            Debug.Log($"Table status: {response.Table.TableStatus}");
            Debug.Log($"Item count: {response.Table.ItemCount}");
        }
        catch (ResourceNotFoundException)
        {
            Debug.LogError($"Table '{TableName}' not found in region '{Region}'. Please create the table first.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to DynamoDB: {e.Message}");
        }
    }

    public async Task SaveScore(string playerName, float timeScore)
    {
        if (!isInitialized)
        {
            Debug.LogError("DynamoDB client not initialized!");
            return;
        }

        try
        {
            var request = new PutItemRequest
            {
                TableName = TableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "PlayerID", new AttributeValue { S = playerName }},
                    { "TimeScore", new AttributeValue { N = timeScore.ToString() }},
                    { "Timestamp", new AttributeValue { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }}
                }
            };

            await client.PutItemAsync(request);
            Debug.Log($"Successfully saved score: {timeScore}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving score: {e.Message}");
        }
    }

    public async Task<List<float>> GetTopScores(int limit = 10)
    {
        if (!isInitialized)
        {
            Debug.LogError("DynamoDB client not initialized!");
            return new List<float>();
        }

        try
        {
            Debug.Log("Attempting to fetch top scores...");

            var request = new ScanRequest
            {
                TableName = TableName,
                ProjectionExpression = "TimeScore",
                Limit = limit
            };

            var response = await client.ScanAsync(request);
            var scores = new List<float>();

            foreach (var item in response.Items)
            {
                if (item.ContainsKey("TimeScore"))
                {
                    float timeScore = float.Parse(item["TimeScore"].N);
                    scores.Add(timeScore);
                }
            }

            // Sort times (ascending - lower times are better)
            scores.Sort();
            Debug.Log($"Retrieved {scores.Count} scores");
            return scores;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting leaderboard: {e.Message}");
            return new List<float>();
        }
    }

    public string FormatTime(float timeInSeconds)
    {
        int minutes = (int)(timeInSeconds / 60);
        int seconds = (int)(timeInSeconds % 60);
        int milliseconds = (int)((timeInSeconds * 100) % 100);
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }
}


